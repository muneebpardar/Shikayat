using Shikayat.Application.DTOs;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Shikayat.Application.Interfaces
{
    public interface IComplaintService
    {
        Task<Complaint> CreateComplaintAsync(ComplaintSubmissionDto model, string userId, string userEmail, string webRootPath);
        Task<string?> UploadFileAsync(IFormFile file, string webRootPath, string folderName);
        Task AddCommentAsync(int complaintId, string userId, string message, bool isSeniorAdmin, bool isZonalAdmin);
        Task UpdateStatusAsync(int id, ComplaintStatus status, string userId, string? resolutionNote, IFormFile? resolutionAttachment, string webRootPath);
        Task ToggleImportanceAsync(int id, bool isImportant);
    }
}
