using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shikayat.Application.DTOs;
using Shikayat.Application.Constants;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;

namespace Shikayat.Web.Controllers
{
    [Authorize]
    public class ComplaintController : Controller
    {
        private readonly ILookupRepository _lookupRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IComplaintService _complaintService;
        private readonly IDashboardService _dashboardService;
        private readonly IComplaintRepository _complaintRepo; // Needed for READ-only details if Service doesn't cover GetById OR we can proxy it.
                                                             // Ideally Service should have GetById. 
                                                             // Checking IComplaintService... It does NOT have GetById.
                                                             // I should add GetById to IComplaintService or reference Repo for Queries (CQRS light).
                                                             // Plan says "ComplaintService orchestrates...".
                                                             // Usage of Repo for READS is acceptable in some Clean Architecture flavours (CQRS), 
                                                             // but to clear the "Hollow" anomaly, we should probably use Service.
                                                             // However, to save time/complexity, I will keep _complaintRepo ONLY for GetById/GetMyComplaints for now,
                                                             // or better: Add GetById to Service? 
                                                             // Let's stick to using Repo for Queries to avoid Proxy methods bloat, concentrating on Logic Removal.
        private readonly IWebHostEnvironment _env;

        public ComplaintController(ILookupRepository lookupRepo,
                                   UserManager<ApplicationUser> userManager,
                                   IComplaintService complaintService,
                                   IDashboardService dashboardService,
                                   IComplaintRepository complaintRepo,
                                   IWebHostEnvironment env)
        {
            _lookupRepo = lookupRepo;
            _userManager = userManager;
            _complaintService = complaintService;
            _dashboardService = dashboardService;
            _complaintRepo = complaintRepo; // Kept for Queries
            _env = env;
        }

        // ============================================================
        //  PART 1: CITIZEN LODGING
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var provinces = await _lookupRepo.GetProvincesAsync();
            var departments = await _lookupRepo.GetDepartmentsAsync();
            ViewBag.Provinces = new SelectList(provinces, "Id", "Name");
            ViewBag.Departments = new SelectList(departments, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComplaintSubmissionDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // DELEGATE TO SERVICE
                var complaint = await _complaintService.CreateComplaintAsync(model, user.Id, user.Email ?? string.Empty, _env.WebRootPath);
                
                TempData["Success"] = "Complaint submitted successfully! Your ticket ID is: " + complaint.TicketId;
                return RedirectToAction("Index");
            }
            catch (ArgumentException ex) // Validation Errors from Service
            {
                ModelState.AddModelError("Attachment", ex.Message);
                ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred: " + ex.Message;
                return View(model);
            }
        }

        // --- AJAX APIs (Keep as is, purely efficient read) ---
        [HttpGet]
        public async Task<JsonResult> GetDistricts(int provinceId) => Json(await _lookupRepo.GetDistrictsAsync(provinceId));

        [HttpGet]
        public async Task<JsonResult> GetTehsils(int districtId) => Json(await _lookupRepo.GetTehsilsAsync(districtId));

        [HttpGet]
        public async Task<JsonResult> GetSubCategories(int departmentId) => Json(await _lookupRepo.GetSubCategoriesAsync(departmentId));


        // ============================================================
        //  PART 2: DASHBOARD & DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(int? drillProvinceId = null, int? drillDistrictId = null, int? drillTehsilId = null, int? departmentId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            
            var roles = await _userManager.GetRolesAsync(user);

            // If Citizen, show list.
            if (roles.Contains(Roles.Citizen))
            {
                var myComplaints = await _complaintRepo.GetComplaintsByCitizenIdAsync(user.Id);
                return View(myComplaints); // Use default Index.cshtml
            }

            // If Admin, redirect to Admin Dashboard
            // It seems Complaint/Index was meant for Citizens. Admins use Admin/Index.
            return RedirectToAction("Index", "Admin");
        }

        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _complaintRepo.GetComplaintByIdAsync(id);
            if (complaint == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            // --- PERMISSION LOGIC ---
            // This permission logic is View-Specific (Controls UI buttons). 
            // It could be moved to a Service that returns a "ComplaintDetailsViewModel" with "CanChat", "CanVote" etc properties.
            // But leaving it here is acceptable for Presentation Logic. 
            // The "Access Control" (Lines 195-212 in original) is CRITICAL logic.
            
            // TODO: Move Access Control Check to Service (e.g., _complaintService.CanView(complaint, user))
            // For now, retaining existing logic to minimize regression risk in this refactor step.

            bool isCitizen = roles.Contains(Roles.Citizen);
            bool isSuper = roles.Contains(Roles.SuperAdmin);
            bool isProv = roles.Contains(Roles.ProvincialAdmin);
            bool isDist = roles.Contains(Roles.DistrictAdmin);
            bool isZonal = roles.Contains(Roles.ZonalAdmin);

            ViewBag.CanChatPublic = isCitizen || isZonal;
            ViewBag.CanLeaveNote = !isCitizen;
            ViewBag.CanMarkImportant = isSuper || isProv || isDist;

            // 4. Access Control
            if (isCitizen && complaint.CitizenId != user.Id) return Forbid();
            
            if (!isCitizen)
            {
                if (!isSuper)
                {
                    if (isProv && complaint.ProvinceId != user.ProvinceId) return Forbid();
                    if (isDist && complaint.DistrictId != user.DistrictId) return Forbid();
                    if (isZonal && complaint.TehsilId != user.TehsilId) return Forbid();
                }
            }

            return View(complaint);
        }

        // ============================================================
        //  PART 3: ACTIONS
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ComplaintId, string Message)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            
            bool isSenior = roles.Contains(Roles.DistrictAdmin) || roles.Contains(Roles.ProvincialAdmin) || roles.Contains(Roles.SuperAdmin);
            bool isZonal = roles.Contains(Roles.ZonalAdmin);
            
            // DELEGATE TO SERVICE
            await _complaintService.AddCommentAsync(ComplaintId, user.Id, Message, isSenior, isZonal);

            TempData["Success"] = "Message sent successfully.";
            return RedirectToAction("Details", new { id = ComplaintId });
        }

        // NEW: Internal Notes (Hidden from Citizen)
        [HttpPost]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.ProvincialAdmin + "," + Roles.DistrictAdmin + "," + Roles.ZonalAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int ComplaintId, string Message)
        {
             // Notes are just comments with different type? 
             // Original: Type = LogType.InternalNote
             // Service needs method for Internal Note? Or update AddCommentAsync to support type?
             // I only added AddCommentAsync for Public.
             // I missed AddInternalNoteAsync in Interface.
             // Quick Fix: Add it to interface or overload. 
             // Logic in Original: InternalNote type.
             
             // I will add a new method to Service for this during this file write? 
             // No, I can't modify Interface easily here. 
             // I will direct use the Repo here for now? NO, that defeats the purpose.
             // I should use _complaintService.AddCommentAsync but I need to handle Type.
             // Refactoring IComplaintService on the fly:
             // I'll assume I can add it, but I already wrote the file.
             
             // WORKAROUND: Use _repo.AddLogAsync here for "InternalNote" as I missed it in Service Plan?
             // Better: Create `AddNoteAsync` in Service in next step if missed.
             // Actually, I'll allow `AddComment` to take a param `isInternal`.
             
             // Wait, for this specific turn, I'll use Repo directly for Note to avoid breaking compilation (Interface mismatch).
             // AND add a TODO to move it.
             
            var user = await _userManager.GetUserAsync(User);
            var log = new ComplaintLog
            {
                ComplaintId = ComplaintId,
                SenderId = user.Id,
                Message = Message,
                Timestamp = DateTime.UtcNow,
                Type = LogType.InternalNote
            };
            await _complaintRepo.AddLogAsync(log); // Direct Repo call (Tech Debt to fix)
            
            TempData["Success"] = "Internal note added successfully.";
            return RedirectToAction("Details", new { id = ComplaintId });
        }

        [HttpPost]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.ProvincialAdmin + "," + Roles.DistrictAdmin + "," + Roles.ZonalAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ComplaintStatus status, string? resolutionNote, IFormFile? resolutionAttachment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            try 
            {
                // DELEGATE TO SERVICE
                await _complaintService.UpdateStatusAsync(id, status, user.Id, resolutionNote, resolutionAttachment, _env.WebRootPath);
                TempData["Success"] = "Status updated successfully.";
            }
            catch(ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
            }
            
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = Roles.SuperAdmin + "," + Roles.ProvincialAdmin + "," + Roles.DistrictAdmin)]
        public async Task<IActionResult> ToggleImportance(int id)
        {
            // We need current state to toggle, or pass desired state?
            // Service method: ToggleImportanceAsync(id, isImportant)
            // Original: fetched, then toggled !isImportant.
            
            // We need to fetch first.
            var c = await _complaintRepo.GetComplaintByIdAsync(id);
            if (c != null)
            {
                await _complaintService.ToggleImportanceAsync(id, !c.IsImportant);
                 TempData["Success"] = !c.IsImportant 
                    ? "Complaint marked as important." 
                    : "Complaint unmarked as important.";
            }

            return RedirectToAction("Details", new { id = id });
        }
    }
}