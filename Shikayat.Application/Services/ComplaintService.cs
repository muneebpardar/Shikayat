using Microsoft.AspNetCore.Http;
using Shikayat.Application.DTOs;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;

namespace Shikayat.Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly IFileStorageService _fileService;
        private readonly IEmailService _emailService;

        public ComplaintService(IComplaintRepository complaintRepo, IFileStorageService fileService, IEmailService emailService)
        {
            _complaintRepo = complaintRepo;
            _fileService = fileService;
            _emailService = emailService;
        }

        public async Task<Complaint> CreateComplaintAsync(ComplaintSubmissionDto model, string userId, string userEmail, string webRootPath)
        {
            string? attachmentPath = null;

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                 if (!_fileService.ValidateFileSize(model.Attachment, 5 * 1024 * 1024))
                 {
                     throw new ArgumentException("File size cannot exceed 5MB.");
                 }

                 var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                 if (!_fileService.ValidateFileType(model.Attachment, allowedExtensions))
                 {
                     throw new ArgumentException("Only image files (JPG, PNG, GIF) and PDF files are allowed.");
                 }

                 attachmentPath = await _fileService.SaveFileAsync(model.Attachment, webRootPath, Path.Combine("uploads", "complaints"));
            }

            var complaint = new Complaint
            {
                TicketId = $"SHK-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}", // NOTE: Still needs fixing (Task 1)
                CitizenId = userId,
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

            // SEND EMAIL NOTIFICATION
            await _emailService.SendEmailAsync(userEmail, "Complaint Received - " + complaint.TicketId,
                $"Dear Citizen,\n\nYour complaint has been registered successfully.\nTicket ID: {complaint.TicketId}\n\nWe will update you shortly.");

            return complaint;
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string webRootPath, string folderName)
        {
             if (file == null || file.Length == 0) return null;
             return await _fileService.SaveFileAsync(file, webRootPath, folderName);
        }

        public async Task AddCommentAsync(int complaintId, string userId, string message, bool isSeniorAdmin, bool isZonalAdmin)
        {
            // Business Rule: Senior Admins get override flag if not Zonal
            if (isSeniorAdmin && !isZonalAdmin)
            {
                message = $"[Override] {message}";
            }

            var log = new ComplaintLog
            {
                ComplaintId = complaintId,
                SenderId = userId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                Type = LogType.Public
            };

            await _complaintRepo.AddLogAsync(log);
        }

        public async Task UpdateStatusAsync(int id, ComplaintStatus status, string userId, string? resolutionNote, IFormFile? resolutionAttachment, string webRootPath)
        {
            // 1. Fetch Complaint to get Citizen Email (before update)
            var existingComplaint = await _complaintRepo.GetComplaintByIdAsync(id);
            if (existingComplaint == null) return;

            string? resolutionAttachmentPath = null;
            if (resolutionAttachment != null)
            {
                 if (!_fileService.ValidateFileSize(resolutionAttachment, 5 * 1024 * 1024))
                     throw new ArgumentException("Resolution attachment size cannot exceed 5MB.");
                 
                 var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
                 if (!_fileService.ValidateFileType(resolutionAttachment, allowedExtensions))
                     throw new ArgumentException("Only image files (JPG, PNG, GIF) and PDF files are allowed.");

                 resolutionAttachmentPath = await _fileService.SaveFileAsync(resolutionAttachment, webRootPath, Path.Combine("uploads", "resolutions"));
            }

            await _complaintRepo.UpdateStatusAsync(id, status, userId, resolutionNote, resolutionAttachmentPath);

            // SEND EMAIL NOTIFICATION
            if (existingComplaint.Citizen != null && !string.IsNullOrEmpty(existingComplaint.Citizen.Email))
            {
                var body = $"Dear {existingComplaint.Citizen.FullName},\n\nYour complaint ({existingComplaint.TicketId}) status has been updated to: {status}.\n";
                if (status == ComplaintStatus.Resolved && !string.IsNullOrEmpty(resolutionNote))
                {
                    body += $"Resolution Note: {resolutionNote}\n";
                }
                
                await _emailService.SendEmailAsync(existingComplaint.Citizen.Email, "Complaint Status Update - " + existingComplaint.TicketId, body);
            }
        }

        public async Task ToggleImportanceAsync(int id, bool isImportant)
        {
            await _complaintRepo.ToggleImportanceAsync(id, isImportant);
        }
    }
}
