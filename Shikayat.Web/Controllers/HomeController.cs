using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Shikayat.Application.Interfaces;
using Shikayat.Web.Models;

namespace Shikayat.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;

        public HomeController(ILogger<HomeController> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> SendTestEmail()
        {
            try
            {
                // Send to self/hardcoded for test
                await _emailService.SendEmailAsync("pardarmuneeb@gmail.com", "Test Email", "This is a connectivity test.");
                return Content("Email sent successfully! If you don't see it, check Spam.");
            }
            catch (Exception ex)
            {
                return Content($"Email Failed: {ex.Message} | {ex.InnerException?.Message}");
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
