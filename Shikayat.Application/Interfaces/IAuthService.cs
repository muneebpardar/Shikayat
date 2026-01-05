using Microsoft.AspNetCore.Identity;
using Shikayat.Application.DTOs;

namespace Shikayat.Application.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Succeeded, IEnumerable<string> Errors)> RegisterAsync(RegisterDto model);
        Task<bool> LoginAsync(LoginDto model);
        Task LogoutAsync();
    }
}
