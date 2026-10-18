namespace DeutschMentor.Application.Admin;

public sealed record AdminUserDto(
    Guid Id, string FullName, string Email, UserRole Role, CefrLevel CurrentLevel, bool IsActive, string SignIn,
    DateTime CreatedAtUtc, DateTime? LastLoginAtUtc, int LessonsCompleted, int Certificates);

public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(string? search, string? role, string? status, int page, CancellationToken ct);
    Task SetActiveAsync(Guid userId, bool active, Guid adminId, CancellationToken ct);
}

public sealed class AdminUserService(IApplicationDbContext db, TimeProvider clock) : IAdminUserService
{
    private const int PageSize = 20;

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(string? search, string? role, string? status, int page, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }

        if (Enum.TryParse<UserRole>(role, true, out var parsedRole) && Enum.IsDefined(parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        if (string.Equals(status, "Locked", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(u => !u.IsActive);
        }
        else if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(u => u.IsActive);
        }

        var current = Math.Max(1, page);
        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((current - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Role,
                u.CurrentLevel,
                u.IsActive,
                HasPassword = u.PasswordHash != null,
                HasGoogle = u.GoogleSubject != null,
                u.CreatedAtUtc,
                u.LastLoginAtUtc,
                Lessons = db.LessonProgress.Count(p => p.UserId == u.Id && p.CompletedAtUtc != null),
                Certificates = db.Certificates.Count(c => c.UserId == u.Id)
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new AdminUserDto(
                r.Id,
                r.FullName,
                r.Email,
                r.Role,
                r.CurrentLevel,
                r.IsActive,
                r.HasPassword && r.HasGoogle ? "Email and Google" : r.HasGoogle ? "Google" : "Email",
                r.CreatedAtUtc.AsUtc(),
                r.LastLoginAtUtc.AsUtc(),
                r.Lessons,
                r.Certificates))
            .ToList();

        return new PagedResult<AdminUserDto>(items, total, current, PageSize);
    }

    public async Task SetActiveAsync(Guid userId, bool active, Guid adminId, CancellationToken ct)
    {
        if (userId == adminId)
        {
            throw new ConflictException("cannot_lock_self", "You cannot lock your own account.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("user_not_found", "User not found.");

        if (user.Role == UserRole.Admin)
        {
            throw new ConflictException("cannot_lock_admin", "Admin accounts cannot be locked here.");
        }

        user.IsActive = active;

        if (!active)
        {
            await db.RevokeRefreshTokensAsync(user.Id, clock.GetUtcNow().UtcDateTime, null, ct);
        }

        await db.SaveChangesAsync(ct);
    }
}