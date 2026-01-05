using Microsoft.AspNetCore.Http;
using Shikayat.Application.Interfaces;

namespace Shikayat.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        public async Task<string> SaveFileAsync(IFormFile file, string webRootPath, string folderName)
        {
            var uploadsFolder = Path.Combine(webRootPath, folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public bool ValidateFileSize(IFormFile file, long maxSizeInBytes)
        {
            return file.Length <= maxSizeInBytes;
        }

        public bool ValidateFileType(IFormFile file, string[] allowedExtensions)
        {
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(fileExtension);
        }
    }
}
