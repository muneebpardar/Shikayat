using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;
using Shikayat.Application.DTOs; // <--- Fix 1: Using the Clean Architecture DTOs
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Shikayat.Infrastructure.Data
{
    public static class DbInitializer
    {
        // context is available HERE 👇
        public static async Task SeedAsync(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await context.Database.MigrateAsync();

            // 1. SEED ROLES
            string[] roleNames = { "SuperAdmin", "ProvincialAdmin", "DistrictAdmin", "ZonalAdmin", "Citizen" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // 2. SEED LOCATIONS FROM JSON
            if (!context.Locations.Any())
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData", "locations.json");

                if (File.Exists(filePath))
                {
                    var jsonData = await File.ReadAllTextAsync(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    // Uses ProvinceDto from Application Layer
                    var provinces = JsonSerializer.Deserialize<List<ProvinceDto>>(jsonData, options);

                    if (provinces != null)
                    {
                        foreach (var provDto in provinces)
                        {
                            var province = new Location { Name = provDto.Name, Type = LocationType.Province };
                            context.Locations.Add(province);
                            await context.SaveChangesAsync();

                            await SeedOfficialUser(userManager, provDto.Name, "ProvincialAdmin", province.Id, null, null);

                            foreach (var distDto in provDto.Districts)
                            {
                                var district = new Location { Name = distDto.Name, Type = LocationType.District, ParentId = province.Id };
                                context.Locations.Add(district);
                                await context.SaveChangesAsync();

                                await SeedOfficialUser(userManager, distDto.Name, "DistrictAdmin", province.Id, district.Id, null);

                                if (distDto.Tehsils != null)
                                {
                                    foreach (var zoneName in distDto.Tehsils)
                                    {
                                        var zone = new Location { Name = zoneName, Type = LocationType.Tehsil, ParentId = district.Id };
                                        context.Locations.Add(zone);
                                        await context.SaveChangesAsync();

                                        await SeedOfficialUser(userManager, zoneName, "ZonalAdmin", province.Id, district.Id, zone.Id);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 3. SEED CATEGORIES FROM JSON
            if (!context.Categories.Any())
            {
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeedData", "categories.json");

                if (File.Exists(filePath))
                {
                    var jsonData = await File.ReadAllTextAsync(filePath);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    var categories = JsonSerializer.Deserialize<List<CategoryDto>>(jsonData, options);

                    if (categories != null)
                    {
                        foreach (var catDto in categories)
                        {
                            var parent = new Category { Name = catDto.Name };
                            context.Categories.Add(parent);
                            await context.SaveChangesAsync();

                            foreach (var subName in catDto.SubCategories)
                            {
                                context.Categories.Add(new Category { Name = subName, ParentId = parent.Id });
                            }
                            await context.SaveChangesAsync();
                        }
                    }
                }
                else
                {
                    // Fallback to hardcoded categories if JSON file doesn't exist
                    var categoryData = new Dictionary<string, List<string>>
                    {
                        { "Health", new List<string> { "Illegal Practice", "Absent Staff", "Sanitation", "Medicine Shortage" } },
                        { "Education", new List<string> { "Absent Teachers", "Broken Furniture", "Illegal Fees" } },
                        { "Municipal Services", new List<string> { "Water Supply", "Sewerage", "Garbage", "Street Lights" } },
                        { "Police", new List<string> { "FIR Issue", "Corruption", "Harassment" } },
                        { "Power", new List<string> { "Load Shedding", "Overbilling", "Transformer Repair" } }
                    };

                    foreach (var cat in categoryData)
                    {
                        var parent = new Category { Name = cat.Key };
                        context.Categories.Add(parent);
                        await context.SaveChangesAsync();

                        foreach (var sub in cat.Value)
                        {
                            context.Categories.Add(new Category { Name = sub, ParentId = parent.Id });
                        }
                        await context.SaveChangesAsync();
                    }
                }
            }

            // 4. SEED SUPER ADMIN
            var adminEmail = "admin@shikayat.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "System Administrator", EmailConfirmed = true, CNIC = "00000-0000000-0" };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }

            // =======================================================
            // 5. SEED COMPLAINTS (Now correctly inside SeedAsync)
            // =======================================================
            if (!context.Complaints.Any())
            {
                // Create multiple dummy citizens for variety
                var citizens = new List<ApplicationUser>();
                for (int i = 1; i <= 10; i++)
                {
                    var citizen = await userManager.FindByEmailAsync($"citizen{i}@shikayat.com");
                    if (citizen == null)
                    {
                        citizen = new ApplicationUser 
                        { 
                            UserName = $"citizen{i}@shikayat.com", 
                            Email = $"citizen{i}@shikayat.com", 
                            FullName = $"Citizen User {i}", 
                            CNIC = $"42101-{i:0000000}-0" 
                        };
                        await userManager.CreateAsync(citizen, "Citizen@123");
                        await userManager.AddToRoleAsync(citizen, "Citizen");
                    }
                    citizens.Add(citizen);
                }

                // Get all locations and categories
                var provinces = await context.Locations.Where(l => l.Type == LocationType.Province).ToListAsync();
                var districts = await context.Locations.Where(l => l.Type == LocationType.District).Include(d => d.Parent).ToListAsync();
                var tehsils = await context.Locations.Where(l => l.Type == LocationType.Tehsil).Include(t => t.Parent).ToListAsync();
                var subCategories = await context.Categories.Where(c => c.ParentId != null).ToListAsync();
                var statuses = new[] { ComplaintStatus.Pending, ComplaintStatus.InProgress, ComplaintStatus.Resolved, ComplaintStatus.Rejected };

                var random = new Random();
                var subjects = new[]
                {
                    "Water Supply Issue", "Power Outage", "Garbage Collection Problem", "Street Light Not Working",
                    "Road Maintenance Required", "Sewerage Blockage", "Illegal Construction", "Noise Complaint",
                    "Traffic Issue", "Public Park Maintenance", "Hospital Facilities", "School Infrastructure",
                    "Police Response", "Tax Issue", "Land Dispute", "License Problem", "Health Emergency",
                    "Education Quality", "Transport Fare Issue", "Internet Problem", "Postal Service Delay"
                };

                var descriptions = new[]
                {
                    "Urgent attention required. This issue has been ongoing for several days and affecting many residents.",
                    "The problem started last week and has not been resolved yet. Please take immediate action.",
                    "This is a recurring issue in our area. We need a permanent solution.",
                    "Multiple residents have reported similar issues. This needs priority attention.",
                    "The situation is getting worse day by day. Immediate intervention required."
                };

                // Generate 150 complaints
                for (int i = 0; i < 150; i++)
                {
                    var province = provinces[random.Next(provinces.Count)];
                    var provinceDistricts = districts.Where(d => d.ParentId == province.Id).ToList();
                    if (!provinceDistricts.Any()) continue;
                    
                    var district = provinceDistricts[random.Next(provinceDistricts.Count)];
                    var districtTehsils = tehsils.Where(t => t.ParentId == district.Id).ToList();
                    if (!districtTehsils.Any()) continue;
                    
                    var tehsil = districtTehsils[random.Next(districtTehsils.Count)];
                    var subCategory = subCategories[random.Next(subCategories.Count)];
                    var citizen = citizens[random.Next(citizens.Count)];
                    var status = statuses[random.Next(statuses.Length)];
                    var subject = subjects[random.Next(subjects.Length)];
                    var description = descriptions[random.Next(descriptions.Length)];

                    var complaint = new Complaint
                    {
                        TicketId = $"SHK-{DateTime.Now.Year}-{random.Next(1000, 9999)}",
                        CitizenId = citizen.Id,
                        Subject = $"{subject} - {province.Name}",
                        Description = description,
                        Status = status,
                        ProvinceId = province.Id,
                        DistrictId = district.Id,
                        TehsilId = tehsil.Id,
                        SubCategoryId = subCategory.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 90)),
                        IsImportant = random.Next(100) < 10, // 10% marked as important
                        Priority = (ComplaintPriority)random.Next(3)
                    };

                    if (status == ComplaintStatus.Resolved)
                    {
                        complaint.ResolvedAt = complaint.CreatedAt.AddDays(random.Next(1, 30));
                        complaint.ResolutionNote = "Issue resolved successfully after investigation.";
                    }

                    context.Complaints.Add(complaint);
                }

                await context.SaveChangesAsync();
            }

        } // <--- END OF SeedAsync METHOD

        // --- HELPER METHOD (Separated correctly) ---
        private static async Task SeedOfficialUser(UserManager<ApplicationUser> userManager, string name, string role, int? pId, int? dId, int? tId)
        {
            string cleanName = Regex.Replace(name, "[^a-zA-Z0-9]", "").ToLower();
            string email = $"{cleanName}@shikayat.com";
            string cleanNameCase = Regex.Replace(name, "[^a-zA-Z0-9]", "");
            string password = $"{cleanNameCase}@123";

            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = $"{name} Admin",
                    EmailConfirmed = true,
                    ProvinceId = pId,
                    DistrictId = dId,
                    TehsilId = tId,
                    CNIC = "00000-0000000-0"
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded) await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}