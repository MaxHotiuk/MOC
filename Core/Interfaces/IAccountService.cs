namespace Core.Interfaces;

public interface IAccountService
{
    Task<string> RegisterUserAsync(string email, string password);
    Task<string> LoginUserAsync(string email, string password, bool rememberMe);
    Task LogoutUserAsync();
}
