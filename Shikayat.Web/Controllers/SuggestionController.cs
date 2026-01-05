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
    public class SuggestionController : Controller
    {
        private readonly ILookupRepository _lookupRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISuggestionRepository _suggestionRepo;
        private readonly IWebHostEnvironment _env;

        public SuggestionController(
            ILookupRepository lookupRepo,
            UserManager<ApplicationUser> userManager,
            ISuggestionRepository suggestionRepo,
            IWebHostEnvironment env)
        {
            _lookupRepo = lookupRepo;
            _userManager = userManager;
            _suggestionRepo = suggestionRepo;
            _env = env;
        }

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
        public async Task<IActionResult> Create(SuggestionSubmissionDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            string? attachmentPath = null;

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                if (model.Attachment.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Attachment", "File size cannot exceed 5MB.");
                    ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                    ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                    return View(model);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                var fileExtension = Path.GetExtension(model.Attachment.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Attachment", "Only image files (JPG, PNG, GIF) and PDF files are allowed.");
                    ViewBag.Provinces = new SelectList(await _lookupRepo.GetProvincesAsync(), "Id", "Name");
                    ViewBag.Departments = new SelectList(await _lookupRepo.GetDepartmentsAsync(), "Id", "Name");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "suggestions");
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

            var suggestion = new Suggestion
            {
                TicketId = $"SUG-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
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

            await _suggestionRepo.AddAsync(suggestion);
            
            TempData["Success"] = "Suggestion submitted successfully! Your ticket ID is: " + suggestion.TicketId;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<JsonResult> GetDistricts(int provinceId) => Json(await _lookupRepo.GetDistrictsAsync(provinceId));

        [HttpGet]
        public async Task<JsonResult> GetTehsils(int districtId) => Json(await _lookupRepo.GetTehsilsAsync(districtId));

        [HttpGet]
        public async Task<JsonResult> GetSubCategories(int departmentId) => Json(await _lookupRepo.GetSubCategoriesAsync(departmentId));

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var mySuggestions = await _suggestionRepo.GetSuggestionsByCitizenIdAsync(user.Id);
            return View(mySuggestions);
        }

        public async Task<IActionResult> Details(int id)
        {
            var suggestion = await _suggestionRepo.GetSuggestionByIdAsync(id);
            if (suggestion == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            bool isCitizen = roles.Contains("Citizen");
            bool isSuper = roles.Contains("SuperAdmin");
            bool isProv = roles.Contains("ProvincialAdmin");
            bool isDist = roles.Contains("DistrictAdmin");
            bool isZonal = roles.Contains("ZonalAdmin");

            ViewBag.CanChatPublic = isCitizen || isZonal;
            ViewBag.CanLeaveNote = !isCitizen;
            ViewBag.CanMarkImportant = isSuper || isProv || isDist;

            if (isCitizen && suggestion.CitizenId != user.Id) return Forbid();
            
            if (!isCitizen)
            {
                if (!isSuper)
                {
                    if (isProv && suggestion.ProvinceId != user.ProvinceId) return Forbid();
                    if (isDist && suggestion.DistrictId != user.DistrictId) return Forbid();
                    if (isZonal && suggestion.TehsilId != user.TehsilId) return Forbid();
                }
            }

            return View(suggestion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int SuggestionId, string Message)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            
            bool isSeniorAdmin = roles.Contains("DistrictAdmin") || 
                                roles.Contains("ProvincialAdmin") || 
                                roles.Contains("SuperAdmin");
            bool isZonal = roles.Contains("ZonalAdmin");
            
            if (isSeniorAdmin && !isZonal)
            {
                Message = $"[Override] {Message}";
            }

            var log = new ComplaintLog
            {
                SuggestionId = SuggestionId,
                SenderId = user.Id,
                Message = Message,
                Timestamp = DateTime.UtcNow,
                Type = LogType.Public
            };

            await _suggestionRepo.AddLogAsync(log);
            TempData["Success"] = "Message sent successfully.";
            return RedirectToAction("Details", new { id = SuggestionId });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,ProvincialAdmin,DistrictAdmin,ZonalAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(int SuggestionId, string Message)
        {
            var user = await _userManager.GetUserAsync(User);

            var log = new ComplaintLog
            {
                SuggestionId = SuggestionId,
                SenderId = user.Id,
                Message = Message,
                Timestamp = DateTime.UtcNow,
                Type = LogType.InternalNote
            };

            await _suggestionRepo.AddLogAsync(log);
            TempData["Success"] = "Internal note added successfully.";
            return RedirectToAction("Details", new { id = SuggestionId });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,ProvincialAdmin,DistrictAdmin,ZonalAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ComplaintStatus status, string? responseNote, IFormFile? responseAttachment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (status == ComplaintStatus.Resolved)
            {
                if (string.IsNullOrWhiteSpace(responseNote))
                {
                    TempData["Error"] = "Response note is required when marking suggestion as resolved.";
                    return RedirectToAction("Details", new { id = id });
                }
            }

            string? responseAttachmentPath = null;
            
            if (responseAttachment != null && responseAttachment.Length > 0)
            {
                if (responseAttachment.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Response attachment size cannot exceed 5MB.";
                    return RedirectToAction("Details", new { id = id });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                var fileExtension = Path.GetExtension(responseAttachment.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["Error"] = "Only image files (JPG, PNG, GIF) and PDF files are allowed.";
                    return RedirectToAction("Details", new { id = id });
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "suggestions", "responses");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await responseAttachment.CopyToAsync(stream);
                }
                
                responseAttachmentPath = fileName;
            }

            await _suggestionRepo.UpdateStatusAsync(id, status, user.Id, responseNote, responseAttachmentPath);

            TempData["Success"] = "Status updated successfully.";
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,ProvincialAdmin,DistrictAdmin")]
        public async Task<IActionResult> ToggleImportance(int id)
        {
            var suggestion = await _suggestionRepo.GetSuggestionByIdAsync(id);
            if (suggestion != null)
            {
                await _suggestionRepo.ToggleImportanceAsync(id, !suggestion.IsImportant);
                TempData["Success"] = suggestion.IsImportant 
                    ? "Suggestion marked as important." 
                    : "Suggestion unmarked as important.";
            }
            return RedirectToAction("Details", new { id = id });
        }
    }
}

