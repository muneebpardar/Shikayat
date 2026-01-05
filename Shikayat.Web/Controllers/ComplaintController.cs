using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shikayat.Application.DTOs;
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
        private readonly IComplaintRepository _complaintRepo;
        private readonly IWebHostEnvironment _env;

        public ComplaintController(ILookupRepository lookupRepo,
                                   UserManager<ApplicationUser> userManager,
                                   IComplaintRepository complaintRepo,
                                   IWebHostEnvironment env)
        {
            _lookupRepo = lookupRepo;
            _userManager = userManager;
            _complaintRepo = complaintRepo;
            _env = env;
        }

        // ============================================================
        //  PART 1: CITIZEN LODGING (KEEPING YOUR EXISTING CODE)
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
            
            string? attachmentPath = null;

            // Handle file upload
            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                // Validate file size (5MB max)
                if (model.Attachment.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Attachment", "File size cannot exceed 5MB.");
                    ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                    ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                    return View(model);
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                var fileExtension = Path.GetExtension(model.Attachment.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Attachment", "Only image files (JPG, PNG, GIF) and PDF files are allowed.");
                    ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                    ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "complaints");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(stream);
                }
                
                attachmentPath = fileName;
            }

            try
            {
                var complaint = new Complaint
                {
                    TicketId = $"SHK-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
                    CitizenId = user.Id,
                    Subject = model.Subject,
                    Description = model.Description,
                    SubCategoryId = model.SelectedSubCategoryId,
                    ProvinceId = model.SelectedProvinceId,
                    DistrictId = model.SelectedDistrictId,
                    TehsilId = model.SelectedTehsilId,
                    AttachmentPath = attachmentPath,
                    Status = ComplaintStatus.Pending,
                    Priority = ComplaintPriority.Normal,
                    CreatedAt = DateTime.UtcNow
                };

                await _complaintRepo.AddAsync(complaint);
                
                TempData["Success"] = "Complaint submitted successfully! Your ticket ID is: " + complaint.TicketId;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " Inner: " + ex.InnerException.Message;
                }
                
                TempData["Error"] = "An error occurred while submitting your complaint: " + errorMessage;
                ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                return View(model);
            }
        }

        // --- AJAX APIs ---
        [HttpGet]
        public async Task<JsonResult> GetDistricts(int provinceId) => Json(await _lookupRepo.GetDistrictsAsync(provinceId));

        [HttpGet]
        public async Task<JsonResult> GetTehsils(int districtId) => Json(await _lookupRepo.GetTehsilsAsync(districtId));

        [HttpGet]
        public async Task<JsonResult> GetSubCategories(int departmentId) => Json(await _lookupRepo.GetSubCategoriesAsync(departmentId));


        // ============================================================
        //  PART 2: DASHBOARD & DETAILS (UPDATED FOR PHASE 4)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var myComplaints = await _complaintRepo.GetComplaintsByCitizenIdAsync(user.Id);
            return View(myComplaints);
        }

        // UPDATED DETAILS: Now includes Logic for Notes/Importance permissions
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _complaintRepo.GetComplaintByIdAsync(id);
            if (complaint == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            // --- PERMISSION LOGIC ---
            bool isCitizen = roles.Contains("Citizen");
            bool isSuper = roles.Contains("SuperAdmin");
            bool isProv = roles.Contains("ProvincialAdmin");
            bool isDist = roles.Contains("DistrictAdmin");
            bool isZonal = roles.Contains("ZonalAdmin");

            // 1. Chat: Only Citizen OR Zonal Admin can chat publicly
            ViewBag.CanChatPublic = isCitizen || isZonal;

            // 2. Notes: All Admins can leave private notes
            ViewBag.CanLeaveNote = !isCitizen;

            // 3. Importance: Only Senior Admins (Dist/Prov/Super) can mark important
            ViewBag.CanMarkImportant = isSuper || isProv || isDist;

            // 4. Access Control: 
            // - Citizen can only see their own
            // - Admins can see complaints in their jurisdiction
            if (isCitizen && complaint.CitizenId != user.Id) return Forbid();
            
            // For admins, check jurisdiction access
            if (!isCitizen)
            {
                // SuperAdmin can see all
                if (!isSuper)
                {
                    // ProvincialAdmin can see their province
                    if (isProv && complaint.ProvinceId != user.ProvinceId) return Forbid();
                    
                    // DistrictAdmin can see their district
                    if (isDist && complaint.DistrictId != user.DistrictId) return Forbid();
                    
                    // ZonalAdmin can see their tehsil
                    if (isZonal && complaint.TehsilId != user.TehsilId) return Forbid();
                }
            }

            return View(complaint);
        }

        // ============================================================
        //  PART 3: ACTIONS (CHAT, STATUS, NOTES, IMPORTANCE)
        // ============================================================

        // UPDATED: Public Chat (Citizen <-> Zonal, with Senior Admin Override)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ComplaintId, string Message)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            
            bool isSeniorAdmin = roles.Contains("DistrictAdmin") || 
                                roles.Contains("ProvincialAdmin") || 
                                roles.Contains("SuperAdmin");
            bool isZonal = roles.Contains("ZonalAdmin");
            
            // Allow senior admins with override flag (they can chat but it's marked)
            if (isSeniorAdmin && !isZonal)
            {
                Message = $"[Override] {Message}";
            }
            else if (isSeniorAdmin && !isZonal)
            {
                // This shouldn't happen, but keep as fallback
                TempData["Error"] = "Senior Admins should use Internal Notes for official communication.";
                return RedirectToAction("Details", new { id = ComplaintId });
            }

            var log = new ComplaintLog
            {
                ComplaintId = ComplaintId,
                SenderId = user.Id,
                Message = Message,
                Timestamp = DateTime.UtcNow,
                Type = LogType.Public
            };

            await _complaintRepo.AddLogAsync(log);
            TempData["Success"] = "Message sent successfully.";
            return RedirectToAction("Details", new { id = ComplaintId });
        }

        // NEW: Internal Notes (Hidden from Citizen)
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,ProvincialAdmin,DistrictAdmin,ZonalAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int ComplaintId, string Message)
        {
            var user = await _userManager.GetUserAsync(User);

            var log = new ComplaintLog
            {
                ComplaintId = ComplaintId,
                SenderId = user.Id,
                Message = Message,
                Timestamp = DateTime.UtcNow,
                Type = LogType.InternalNote // <--- Hidden
            };

            await _complaintRepo.AddLogAsync(log);
            TempData["Success"] = "Internal note added successfully.";
            return RedirectToAction("Details", new { id = ComplaintId });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,ProvincialAdmin,DistrictAdmin,ZonalAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ComplaintStatus status, string? resolutionNote, IFormFile? resolutionAttachment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // If resolving, require resolution note
            if (status == ComplaintStatus.Resolved)
            {
                if (string.IsNullOrWhiteSpace(resolutionNote))
                {
                    TempData["Error"] = "Resolution note is required when marking complaint as resolved.";
                    return RedirectToAction("Details", new { id = id });
                }
            }

            string? resolutionAttachmentPath = null;
            
            // Handle resolution attachment
            if (resolutionAttachment != null && resolutionAttachment.Length > 0)
            {
                // Validate file size (5MB max)
                if (resolutionAttachment.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Resolution attachment size cannot exceed 5MB.";
                    return RedirectToAction("Details", new { id = id });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                var fileExtension = Path.GetExtension(resolutionAttachment.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only image files (JPG, PNG, GIF) and PDF files are allowed.";
                    return RedirectToAction("Details", new { id = id });
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "resolutions");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await resolutionAttachment.CopyToAsync(stream);
                }
                
                resolutionAttachmentPath = fileName;
            }

            await _complaintRepo.UpdateStatusAsync(id, status, user.Id, resolutionNote, resolutionAttachmentPath);

            TempData["Success"] = "Status updated successfully.";
            return RedirectToAction("Details", new { id = id });
        }

        // NEW: Toggle Importance (Senior Admins Only)
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,ProvincialAdmin,DistrictAdmin")]
        public async Task<IActionResult> ToggleImportance(int id)
        {
            var complaint = await _complaintRepo.GetComplaintByIdAsync(id);
            if (complaint != null)
            {
                await _complaintRepo.ToggleImportanceAsync(id, !complaint.IsImportant);
                TempData["Success"] = complaint.IsImportant 
                    ? "Complaint marked as important." 
                    : "Complaint unmarked as important.";
            }
            return RedirectToAction("Details", new { id = id });
        }
    }
}