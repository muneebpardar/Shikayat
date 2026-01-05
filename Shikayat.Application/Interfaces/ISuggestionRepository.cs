using Shikayat.Domain.Entities;

namespace Shikayat.Application.Interfaces
{
    public interface ISuggestionRepository
    {
        Task AddAsync(Suggestion suggestion);
        Task<Suggestion> GetSuggestionByIdAsync(int id);
        Task<List<Suggestion>> GetSuggestionsByCitizenIdAsync(string citizenId);
        Task<List<Suggestion>> GetSuggestionsByJurisdictionAsync(int? provinceId, int? districtId, int? tehsilId);
        Task UpdateStatusAsync(int suggestionId, Domain.Enums.ComplaintStatus status, string userId, string? responseNote = null, string? responseAttachmentPath = null);
        Task ToggleImportanceAsync(int suggestionId, bool isImportant);
        Task AddLogAsync(ComplaintLog log);
    }
}

