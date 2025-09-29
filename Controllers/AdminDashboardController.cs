using FoodOrderWeb.Models;
using FoodOrderWeb.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly FoodorderwebContext _context = new FoodorderwebContext();

        public AdminDashboardController(FoodorderwebContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var totalUsers = _context.Users.Count();
            var totalOrders = _context.Orders.Count();
            var totalRevenue = _context.Orders
                            .Include(o => o.OrderStatus)
                            .Where(o => o.OrderStatus.Name == "Completed").Sum(o => o.TotalAmount);
            var totalProducts = _context.MenuItems.Count();
            var totalCategories = _context.Categories.Count();

            var viewModel = new AdminViewModel
            {
                TotalUsers = totalUsers,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalCategories = totalCategories,
                TotalProducts = totalProducts
            };
            return View(viewModel);
        }
    }
}
