using Microsoft.AspNetCore.Mvc;
using Shikayat.Application.DTOs;
using Shikayat.Application.Interfaces;

namespace Shikayat.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (ModelState.IsValid)
            {
                var (succeeded, errors) = await _authService.RegisterAsync(model);

                if (succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                // If failure, add errors to UI
                foreach (var error in errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (ModelState.IsValid)
            {
                var succeeded = await _authService.LoginAsync(model);
                if (succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}