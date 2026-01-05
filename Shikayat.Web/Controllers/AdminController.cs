using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;

namespace Shikayat.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin, ProvincialAdmin, DistrictAdmin, ZonalAdmin")]
    public class AdminController : Controller
    {
        private readonly IComplaintRepository _repo;
        private readonly ISuggestionRepository _suggestionRepo;
        private readonly ILookupRepository _lookupRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(IComplaintRepository repo, ISuggestionRepository suggestionRepo, ILookupRepository lookupRepo, UserManager<ApplicationUser> userManager)
        {
            _repo = repo;
            _suggestionRepo = suggestionRepo;
            _lookupRepo = lookupRepo;
            _userManager = userManager;
        }

        // UPDATED: Now accepts optional Drill-Down IDs (pId = ProvinceId, dId = DistrictId) and department filter
        public async Task<IActionResult> Index(int? pId, int? dId, int? tId, int? departmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);

            // Pass tId to repo
            var dashboardData = await _repo.GetDashboardStatsAsync(user, roles, pId, dId, tId, departmentId);

            // Add suggestion stats
            var suggestions = await _suggestionRepo.GetSuggestionsByJurisdictionAsync(
                roles.Contains("ProvincialAdmin") ? user.ProvinceId : pId,
                roles.Contains("DistrictAdmin") ? user.DistrictId : dId,
                roles.Contains("ZonalAdmin") ? user.TehsilId : tId);
            
            dashboardData.TotalSuggestions = suggestions.Count;
            dashboardData.ResolvedSuggestions = suggestions.Count(s => s.Status == ComplaintStatus.Resolved);
            dashboardData.PendingSuggestions = suggestions.Count(s => s.Status != ComplaintStatus.Resolved);

            ViewBag.CurrentProvId = pId;
            ViewBag.CurrentDistId = dId;
            ViewBag.CurrentTehsilId = tId;
            ViewBag.SelectedDepartmentId = departmentId;
            
            // For ProvincialAdmin, store their province ID so the view can use it for drill-down links
            if (roles.Contains("ProvincialAdmin") && user.ProvinceId.HasValue)
            {
                ViewBag.UserProvinceId = user.ProvinceId.Value;
            }
            
            // Add departments for filter (for all admin types)
            ViewBag.Departments = await _lookupRepo.GetDepartmentsAsync();
            ViewBag.ShowDepartmentFilter = true;

            return View(dashboardData);
        }

        // NEW: All Complaints List Page (Based on Admin Jurisdiction)
        public async Task<IActionResult> AllComplaints(int? departmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }
            var roles = await _userManager.GetRolesAsync(user);

            int? provinceId = null;
            int? districtId = null;
            int? tehsilId = null;

            // Determine jurisdiction based on role
            if (roles.Contains("ProvincialAdmin"))
                provinceId = user.ProvinceId;
            else if (roles.Contains("DistrictAdmin"))
                districtId = user.DistrictId;
            else if (roles.Contains("ZonalAdmin"))
                tehsilId = user.TehsilId;
            // SuperAdmin sees all (no filter)

            var complaints = await _repo.GetComplaintsByJurisdictionAsync(provinceId, districtId, tehsilId);

            // Filter by department if specified
            if (departmentId.HasValue)
            {
                complaints = complaints.Where(c => c.SubCategory.ParentId == departmentId).ToList();
            }

            ViewBag.Departments = await _lookupRepo.GetDepartmentsAsync();
            ViewBag.SelectedDepartmentId = departmentId;
            ViewBag.UserRole = roles.FirstOrDefault();

            return View(complaints);
        }

        // NEW: Send Intervention Message
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendIntervention(int regionId, string userRole, string message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("SuperAdmin") && !roles.Contains("ProvincialAdmin"))
            {
                return Forbid();
            }

            // Get complaints for the region
            List<Complaint> complaints;
            if (userRole == "SuperAdmin")
            {
                // SuperAdmin intervening at province level
                complaints = await _repo.GetComplaintsByJurisdictionAsync(regionId, null, null);
            }
            else if (userRole == "ProvincialAdmin")
            {
                // ProvincialAdmin intervening at district level
                complaints = await _repo.GetComplaintsByJurisdictionAsync(user.ProvinceId, regionId, null);
            }
            else
            {
                return BadRequest("Invalid user role");
            }

            // Create intervention log for first complaint in the region (as a representative)
            // In a real system, you might want to create a separate Intervention entity
            // For now, we'll use the complaint log system with a special marker
            if (complaints.Any())
            {
                var firstComplaint = complaints.First();
                var interventionMessage = $"[INTERVENTION - {user.FullName}] {message}";
                
                var log = new ComplaintLog
                {
                    ComplaintId = firstComplaint.Id,
                    SenderId = user.Id,
                    Message = interventionMessage,
                    Timestamp = DateTime.UtcNow,
                    Type = LogType.InternalNote
                };

                await _repo.AddLogAsync(log);
                TempData["Success"] = "Intervention message sent successfully!";
            }
            else
            {
                TempData["Info"] = "No complaints found in this region.";
            }

            return Ok();
        }

    }
}