using System.Diagnostics;
using FoodOrderWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FoodorderwebContext _context = new FoodorderwebContext();

        public HomeController(ILogger<HomeController> logger, FoodorderwebContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
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
