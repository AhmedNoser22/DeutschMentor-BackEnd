namespace DeutschMentor.Application.Authentication;

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ICodeService codes,
    ITokenService tokens,
    IGoogleTokenValidator googleValidator,
    IEmailSender emailSender,
    TimeProvider clock) : IAuthService
{
    public async Task<PendingRegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = AuthValidator.NormalizeEmail(request.Email);
        var fullName = AuthValidator.NormalizeName(request.FullName);
        AuthValidator.EnsureValidPassword(request.Password);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            throw new ConflictException("email_taken", "An account with this email already exists.");
        }

        var now = Now();
        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (pending is null)
        {
            pending = new PendingRegistration { Email = email };
            db.PendingRegistrations.Add(pending);
        }
        else
        {
            EnsureCooldown(pending.LastSentAtUtc, now);
        }

        var code = codes.Generate();
        pending.FullName = fullName;
        pending.PasswordHash = passwordHasher.Hash(request.Password!);
        pending.StartPreference = request.StartPreference;
        pending.CodeHash = codes.Hash(email, code);
        pending.ExpiresAtUtc = now + AuthRules.CodeLifetime;
        pending.LastSentAtUtc = now;
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync(ct);

        await SendAsync(email, EmailTemplates.Verification(fullName, code, CodeMinutes), ct);
        return CodeSentResponse(email);
    }

    public async Task<AuthResponse> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct)
    {
        var email = AuthValidator.NormalizeEmail(request.Email);
        var now = Now();

        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct)
            ?? throw new BadRequestException("invalid_code", "The code is not correct.");

        if (pending.ExpiresAtUtc <= now)
        {
            throw new BadRequestException("code_expired", "This code has expired. Request a new one.");
        }

        if (pending.FailedAttempts >= AuthRules.MaxCodeAttempts)
        {
            throw new TooManyRequestsException("too_many_attempts", "Too many wrong attempts. Request a new code.");
        }

        if (!codes.Verify(email, request.Code ?? string.Empty, pending.CodeHash))
        {
            pending.FailedAttempts++;
            await db.SaveChangesAsync(ct);
            throw new BadRequestException("invalid_code", "The code is not correct.");
        }

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
        {
            db.PendingRegistrations.Remove(pending);
            await db.SaveChangesAsync(ct);
            throw new ConflictException("email_taken", "An account with this email already exists.");
        }

        var user = new User
        {
            FullName = pending.FullName,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            StartPreference = pending.StartPreference,
            EmailConfirmedAtUtc = now
        };

        db.Users.Add(user);
        db.PendingRegistrations.Remove(pending);
        return await IssueAuthAsync(user, now, ct);
    }

    public async Task<PendingRegistrationResponse> ResendCodeAsync(ResendCodeRequest request, CancellationToken ct)
    {
        var email = AuthValidator.NormalizeEmail(request.Email);
        var now = Now();

        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct)
            ?? throw new BadRequestException("no_pending_registration", "Start the registration again.");

        EnsureCooldown(pending.LastSentAtUtc, now);

        var code = codes.Generate();
        pending.CodeHash = codes.Hash(email, code);
        pending.ExpiresAtUtc = now + AuthRules.CodeLifetime;
        pending.LastSentAtUtc = now;
        pending.FailedAttempts = 0;
        await db.SaveChangesAsync(ct);

        await SendAsync(email, EmailTemplates.Verification(pending.FullName, code, CodeMinutes), ct);
        return CodeSentResponse(email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = AuthValidator.NormalizeEmail(request.Email);
        var password = request.Password ?? string.Empty;
        var invalid = new UnauthorizedException("invalid_credentials", "Email or password is not correct.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
            if (pending is not null && passwordHasher.Verify(password, pending.PasswordHash))
            {
                throw new ForbiddenException("email_not_confirmed", "Confirm your email to finish creating your account.");
            }

            throw invalid;
        }

        if (user.PasswordHash is null)
        {
            throw new UnauthorizedException("use_google", "This account uses Google sign-in. Continue with Google, or reset your password to add one.");
        }

        if (!passwordHasher.Verify(password, user.PasswordHash))
        {
            throw invalid;
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("account_disabled", "This account is disabled.");
        }

        return await IssueAuthAsync(user, Now(), ct);
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct)
    {
        var info = await googleValidator.ValidateAsync(request.IdToken ?? string.Empty, ct);
        if (!info.EmailVerified)
        {
            throw new UnauthorizedException("google_email_unverified", "Your Google email is not verified.");
        }

        var email = AuthValidator.NormalizeEmail(info.Email);
        var now = Now();

        var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == info.Subject || u.Email == email, ct);
        if (user is null)
        {
            user = new User
            {
                FullName = AuthValidator.FallbackName(info.FullName, email),
                Email = email,
                GoogleSubject = info.Subject,
                StartPreference = request.StartPreference ?? StartPreference.StartAtA1,
                EmailConfirmedAtUtc = now
            };
            db.Users.Add(user);
        }
        else if (user.GoogleSubject is null)
        {
            user.GoogleSubject = info.Subject;
        }
        else if (user.GoogleSubject != info.Subject)
        {
            throw new UnauthorizedException("google_account_mismatch", "This email is linked to a different Google account.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("account_disabled", "This account is disabled.");
        }

        var pending = await db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (pending is not null)
        {
            db.PendingRegistrations.Remove(pending);
        }

        return await IssueAuthAsync(user, now, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var now = Now();
        var hash = tokens.HashRefreshToken(request.RefreshToken ?? string.Empty);
        var invalid = new UnauthorizedException("invalid_refresh_token", "Your session has expired. Sign in again.");

        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored?.User is null)
        {
            throw invalid;
        }

        if (stored.RevokedAtUtc is not null)
        {
            await db.RevokeRefreshTokensAsync(stored.UserId, now, null, ct);
            await db.SaveChangesAsync(ct);
            throw invalid;
        }

        if (stored.ExpiresAtUtc <= now || !stored.User.IsActive)
        {
            throw invalid;
        }

        stored.RevokedAtUtc = now;
        return await IssueAuthAsync(stored.User, now, ct);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken ?? string.Empty);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAtUtc: null })
        {
            stored.RevokedAtUtc = Now();
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var email = AuthValidator.NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
        if (user is null)
        {
            return;
        }

        var now = Now();
        var entry = await db.PasswordResetCodes.FirstOrDefaultAsync(c => c.UserId == user.Id, ct);
        if (entry is null)
        {
            entry = new PasswordResetCode { UserId = user.Id };
            db.PasswordResetCodes.Add(entry);
        }
        else if (now - entry.LastSentAtUtc < AuthRules.ResendCooldown)
        {
            return;
        }

        var code = codes.Generate();
        entry.CodeHash = codes.Hash(email, code);
        entry.ExpiresAtUtc = now + AuthRules.CodeLifetime;
        entry.LastSentAtUtc = now;
        entry.FailedAttempts = 0;
        await db.SaveChangesAsync(ct);

        await SendAsync(email, EmailTemplates.PasswordReset(code, CodeMinutes), ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        AuthValidator.EnsureValidPassword(request.NewPassword);
        var email = AuthValidator.NormalizeEmail(request.Email);
        var now = Now();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var entry = user is null
            ? null
            : await db.PasswordResetCodes.FirstOrDefaultAsync(c => c.UserId == user.Id, ct);

        if (user is null || entry is null)
        {
            throw new BadRequestException("invalid_code", "The code is not correct.");
        }

        if (entry.ExpiresAtUtc <= now)
        {
            throw new BadRequestException("code_expired", "This code has expired. Request a new one.");
        }

        if (entry.FailedAttempts >= AuthRules.MaxCodeAttempts)
        {
            throw new TooManyRequestsException("too_many_attempts", "Too many wrong attempts. Request a new code.");
        }

        if (!codes.Verify(email, request.Code ?? string.Empty, entry.CodeHash))
        {
            entry.FailedAttempts++;
            await db.SaveChangesAsync(ct);
            throw new BadRequestException("invalid_code", "The code is not correct.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword!);
        db.PasswordResetCodes.Remove(entry);
        await db.RevokeRefreshTokensAsync(user.Id, now, null, ct);
        await db.SaveChangesAsync(ct);
    }

    private static int CodeMinutes => (int)AuthRules.CodeLifetime.TotalMinutes;

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static PendingRegistrationResponse CodeSentResponse(string email) => new(
        email,
        (int)AuthRules.CodeLifetime.TotalSeconds,
        (int)AuthRules.ResendCooldown.TotalSeconds);

    private static void EnsureCooldown(DateTime lastSentAtUtc, DateTime nowUtc)
    {
        var wait = AuthRules.ResendCooldown - (nowUtc - lastSentAtUtc);
        if (wait > TimeSpan.Zero)
        {
            throw new TooManyRequestsException(
                "resend_too_soon",
                $"Please wait {(int)Math.Ceiling(wait.TotalSeconds)} seconds before requesting another code.");
        }
    }

    private Task SendAsync(string to, EmailContent content, CancellationToken ct) =>
        emailSender.SendAsync(to, content.Subject, content.HtmlBody, ct);

    private async Task<AuthResponse> IssueAuthAsync(User user, DateTime nowUtc, CancellationToken ct)
    {
        var rawRefreshToken = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(rawRefreshToken),
            ExpiresAtUtc = nowUtc + AuthRules.RefreshTokenLifetime
        });

        user.LastLoginAtUtc = nowUtc;
        await db.SaveChangesAsync(ct);

        var access = tokens.CreateAccessToken(user);
        return new AuthResponse(access.Value, access.ExpiresAtUtc, rawRefreshToken, UserMapper.ToDto(user));
    }
}