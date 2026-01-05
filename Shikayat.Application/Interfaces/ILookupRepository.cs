using Shikayat.Domain.Entities;

namespace Shikayat.Application.Interfaces
{
    public interface ILookupRepository
    {
        Task<List<Location>> GetProvincesAsync();
        Task<List<Location>> GetDistrictsAsync(int provinceId);
        Task<List<Location>> GetTehsilsAsync(int districtId);
        Task<List<Category>> GetDepartmentsAsync(); // L1
        Task<List<Category>> GetSubCategoriesAsync(int departmentId); // L2
    }
}