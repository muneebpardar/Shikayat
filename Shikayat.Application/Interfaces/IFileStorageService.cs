using Microsoft.AspNetCore.Http;

namespace Shikayat.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string webRootPath, string folderName);
        bool ValidateFileSize(IFormFile file, long maxSizeInBytes);
        bool ValidateFileType(IFormFile file, string[] allowedExtensions);
    }
}
