using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services
{
    public class AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

        public async Task<string> RegisterUserAsync(string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return "Success";
            }

            return string.Join(", ", result.Errors.Select(e => e.Description));
        }

        public async Task<string> LoginUserAsync(string email, string password, bool rememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return "Success";
            }

            if (result.IsLockedOut)
            {
                return "User is locked out.";
            }
            if (result.IsNotAllowed)
            {
                return "User is not allowed to sign in.";
            }
            if (result.RequiresTwoFactor)
            {
                return "Two-factor authentication is required.";
            }
            return "Invalid email or password.";
        }

        public async Task LogoutUserAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}