using Microsoft.EntityFrameworkCore;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;
using Shikayat.Infrastructure.Data;

namespace Shikayat.Infrastructure.Repositories
{
    public class LookupRepository : ILookupRepository
    {
        private readonly AppDbContext _context;

        public LookupRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Location>> GetProvincesAsync()
        {
            return await _context.Locations
                .Where(x => x.Type == LocationType.Province)
                .ToListAsync();
        }

        public async Task<List<Location>> GetDistrictsAsync(int provinceId)
        {
            return await _context.Locations
                .Where(x => x.ParentId == provinceId && x.Type == LocationType.District)
                .ToListAsync();
        }

        public async Task<List<Location>> GetTehsilsAsync(int districtId)
        {
            return await _context.Locations
                .Where(x => x.ParentId == districtId && x.Type == LocationType.Tehsil)
                .ToListAsync();
        }

        public async Task<List<Category>> GetDepartmentsAsync()
        {
            // L1 categories have NO ParentId
            return await _context.Categories
                .Where(x => x.ParentId == null)
                .ToListAsync();
        }

        public async Task<List<Category>> GetSubCategoriesAsync(int departmentId)
        {
            return await _context.Categories
                .Where(x => x.ParentId == departmentId)
                .ToListAsync();
        }
    }
}
