using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;

namespace Shikayat.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin, ProvincialAdmin")]
    public class AnalyticsController : Controller
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsController(IComplaintRepository complaintRepo, UserManager<ApplicationUser> userManager)
        {
            _complaintRepo = complaintRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var analytics = await _complaintRepo.GetAnalyticsAsync(user, roles);

            return View(analytics);
        }
    }
}

