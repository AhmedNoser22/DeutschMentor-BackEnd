namespace DeutschMentor.Application.Authentication;

internal static class UserMapper
{
    public static UserDto ToDto(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.Role,
        user.CurrentLevel,
        user.StartPreference,
        user.PasswordHash is not null);
}