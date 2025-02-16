using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using MVC.Models;

namespace MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.RegisterUserAsync(model.Email, model.Password);

                if (result == "Success")
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, result);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.LoginUserAsync(model.Email, model.Password, model.RememberMe);

                if (result == "Success")
                {
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, result);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutUserAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}