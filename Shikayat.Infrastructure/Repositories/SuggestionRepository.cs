using Microsoft.EntityFrameworkCore;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;
using Shikayat.Infrastructure.Data;

namespace Shikayat.Infrastructure.Repositories
{
    public class SuggestionRepository : ISuggestionRepository
    {
        private readonly AppDbContext _context;

        public SuggestionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Suggestion suggestion)
        {
            await _context.Suggestions.AddAsync(suggestion);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Suggestion>> GetSuggestionsByCitizenIdAsync(string citizenId)
        {
            return await _context.Suggestions
                .Include(s => s.SubCategory)
                .ThenInclude(sc => sc.Parent)
                .Where(s => s.CitizenId == citizenId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Suggestion> GetSuggestionByIdAsync(int id)
        {
            var suggestion = await _context.Suggestions
                .Include(s => s.Citizen)
                .Include(s => s.SubCategory)
                .Include(s => s.Province)
                .Include(s => s.District)
                .Include(s => s.Tehsil)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (suggestion != null)
            {
                suggestion.Logs = await _context.ComplaintLogs
                    .Where(l => l.SuggestionId == id)
                    .Include(l => l.Sender)
                    .ToListAsync();
            }
            
            return suggestion;
        }

        public async Task<List<Suggestion>> GetSuggestionsByJurisdictionAsync(int? provinceId, int? districtId, int? tehsilId)
        {
            var query = _context.Suggestions
                .Include(s => s.Citizen)
                .Include(s => s.SubCategory)
                .Include(s => s.Province)
                .Include(s => s.District)
                .AsQueryable();

            if (provinceId.HasValue)
                query = query.Where(s => s.ProvinceId == provinceId);

            if (districtId.HasValue)
                query = query.Where(s => s.DistrictId == districtId);

            if (tehsilId.HasValue)
                query = query.Where(s => s.TehsilId == tehsilId);

            return await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        }

        public async Task UpdateStatusAsync(int suggestionId, ComplaintStatus status, string userId, string? responseNote = null, string? responseAttachmentPath = null)
        {
            var suggestion = await _context.Suggestions.FindAsync(suggestionId);
            if (suggestion != null)
            {
                var oldStatus = suggestion.Status;
                suggestion.Status = status;

                if (status == ComplaintStatus.Resolved)
                {
                    suggestion.ResolvedAt = DateTime.UtcNow;
                    suggestion.ResponseNote = responseNote;
                    suggestion.ResponseAttachmentPath = responseAttachmentPath;
                }
                else if (oldStatus == ComplaintStatus.Resolved && status != ComplaintStatus.Resolved)
                {
                    suggestion.ResolvedAt = null;
                    suggestion.ResponseNote = null;
                    suggestion.ResponseAttachmentPath = null;
                }

                var logMessage = $"Status updated to {status}";
                if (status == ComplaintStatus.Resolved && !string.IsNullOrEmpty(responseNote))
                {
                    logMessage += $": {responseNote}";
                }

                _context.ComplaintLogs.Add(new ComplaintLog
                {
                    SuggestionId = suggestionId,
                    Message = logMessage,
                    Timestamp = DateTime.UtcNow,
                    Type = LogType.StatusChange,
                    SenderId = userId
                });

                await _context.SaveChangesAsync();
            }
        }

        public async Task ToggleImportanceAsync(int suggestionId, bool isImportant)
        {
            var suggestion = await _context.Suggestions.FindAsync(suggestionId);
            if (suggestion != null)
            {
                suggestion.IsImportant = isImportant;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddLogAsync(ComplaintLog log)
        {
            await _context.ComplaintLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }
}

