using FoodOrderWeb.Models;
using FoodOrderWeb.Models.ViewModel;
using FoodOrderWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FoodOrderWeb.Controllers
{
    public class CategoryController : Controller
    {
        private readonly FoodorderwebContext _context;
        public CategoryController(FoodorderwebContext context)
        {
            _context = context;
        }

        public IActionResult CategoryName(int id)
        {
            // Lấy Category + MenuItems + Sizes
            var category = _context.Categories
                .Include(c => c.MenuItems)
                    .ThenInclude(mi => mi.MenuItemSizes)
                .FirstOrDefault(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            // Voucher áp dụng cho MenuItems trong Category
            var voucherMenuItems = _context.Vouchers
                .Where(v => v.IsActive && v.EndDate >= DateTime.Now)
                .Where(v => v.MenuItems.Any(mi => mi.CategoryId == id));

            // Voucher áp dụng cho MenuItemSizes trong Category
            var voucherSizes = _context.Vouchers
                .Where(v => v.IsActive && v.EndDate >= DateTime.Now)
                .Where(v => v.MenuItemSizes.Any(ms => ms.MenuItem.CategoryId == id));

            // Voucher áp dụng cho Combos có món thuộc Category
            var voucherCombos = _context.Vouchers
                .Where(v => v.IsActive && v.EndDate >= DateTime.Now)
                .Where(v => v.Combos.Any(c =>
                    c.ComboItems.Any(ci => ci.MenuItem.CategoryId == id)));

            // Gộp tất cả vouchers lại (Distinct)
            var vouchers = voucherMenuItems
                .Union(voucherSizes)
                .Union(voucherCombos)
                .Distinct()
                .Include(v => v.MenuItems)
                .Include(v => v.MenuItemSizes)
                .Include(v => v.Combos)
                .ToList();

            var categories = _context.Categories.ToList();

            var viewModel = new CategoryForHomeViewModel
            {
                Category = category,
                Vouchers = vouchers,    
                Categories = categories,

            };

            return View(viewModel);
        }
    }
}
