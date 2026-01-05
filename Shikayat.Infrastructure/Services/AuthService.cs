using Microsoft.AspNetCore.Identity;
using Shikayat.Application.Constants;
using Shikayat.Application.DTOs;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;

namespace Shikayat.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterDto model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CNIC = model.CNIC
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.Citizen);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return (true, Enumerable.Empty<string>());
            }

            return (false, result.Errors.Select(e => e.Description));
        }

        public async Task<bool> LoginAsync(LoginDto model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            return result.Succeeded;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
