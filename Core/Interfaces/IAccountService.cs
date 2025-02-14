namespace Core.Interfaces;

public interface IAccountService
{
    Task<bool> RegisterUserAsync(string email, string password);
    Task<bool> LoginUserAsync(string email, string password, bool rememberMe);
    Task LogoutUserAsync();
}
