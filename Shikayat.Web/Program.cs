using Microsoft.EntityFrameworkCore;
using Shikayat.Infrastructure.Data;
using Shikayat.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Shikayat.Application.Interfaces;
using Shikayat.Infrastructure.Repositories;
using Shikayat.Application.Services;
using Shikayat.Infrastructure.Services;


var builder = WebApplication.CreateBuilder(args);

// 1. Get Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Register Database Context (Connects to SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Register Identity (Manages Users & Roles)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings (optional - makes testing easier)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ILookupRepository, LookupRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<ISuggestionRepository, SuggestionRepository>();

// Service Layer
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// --- START SEEDING ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// --- END SEEDING ---

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
