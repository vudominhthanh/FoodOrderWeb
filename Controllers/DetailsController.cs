using FoodOrderWeb.Models;
using FoodOrderWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
public class DetailsController : Controller
{
    private readonly FoodorderwebContext _context;

    public DetailsController(FoodorderwebContext context)
    {
        _context = context;
    }

    // Chi tiết Combo
    public IActionResult ComboDetails(int id)
    {
        // Lấy combo + món trong combo
        var combo = _context.Combos
            .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.MenuItem)
            .Include(c => c.Vouchers) // navigation many-to-many từ Combo sang Voucher
            .FirstOrDefault(c => c.ComboId == id);

        if (combo == null) return NotFound();

        // Lấy ngẫu nhiên 3 combo khác
        var otherCombos = _context.Combos
            .Where(c => c.ComboId != id)
            .OrderBy(r => Guid.NewGuid())
            .Take(3)
            .ToList();

        // Lấy 4 danh mục gợi ý
        var categories = _context.Categories.Take(4).ToList();

        // Lấy danh sách voucher liên quan combo này
        var vouchers = combo.Vouchers.ToList();

        // Truyền sang View
        ViewBag.OtherCombos = otherCombos;
        ViewBag.Categories = categories;
        ViewBag.ComboVouchers = vouchers;

        return View(combo);
    }

    // Chi tiết món ăn
    public IActionResult FoodDetails(int id)
    {
        var food = _context.MenuItems
            .Include(mi => mi.Category)
            .Include(mi => mi.MenuItemSizes)
            .FirstOrDefault(mi => mi.MenuItemId == id);

        if (food == null) return NotFound();

        // Lấy ngẫu nhiên 5 món khác cùng Category
        var relatedFoods = _context.MenuItems
            .Where(mi => mi.CategoryId == food.CategoryId && mi.MenuItemId != id)
            .OrderBy(r => Guid.NewGuid())
            .Take(5)
            .ToList();

        // Voucher áp dụng cho món này
        var vouchersForFood = _context.Vouchers
            .Include(v => v.MenuItems)
            .Where(v => v.MenuItems.Any(mi => mi.MenuItemId == food.MenuItemId))
            .ToList();

        // Voucher áp dụng cho danh mục của món này
        var vouchersForCategory = _context.Vouchers
            .Include(v => v.MenuItems)
            .Where(v => v.MenuItems.Any(mi => mi.CategoryId == food.CategoryId))
            .ToList();

        // Combo có chứa món này
        var combosWithFood = _context.Combos
             .Include(c => c.ComboItems)
                 .ThenInclude(ci => ci.MenuItem)
             .Where(c => c.ComboItems.Any(ci => ci.MenuItemId == food.MenuItemId))
             .ToList();


        // Toàn bộ danh mục
        var categories = _context.Categories.ToList();

        var menuItems = _context.MenuItems
            .Include(mi => mi.Category)
            .ToList();

        ViewBag.MenuItems = menuItems;
        ViewBag.RelatedFoods = relatedFoods;
        ViewBag.VouchersForFood = vouchersForFood;
        ViewBag.VouchersForCategory = vouchersForCategory;
        ViewBag.Categories = categories;
        ViewBag.CombosWithFood = combosWithFood;

        return View(food);
    }   

    // Chi tiết danh mục
    public IActionResult CategoryDetails(int id)
    {
        var category = _context.Categories
            .Include(c => c.MenuItems)
            .FirstOrDefault(c => c.CategoryId == id);

        if (category == null) return NotFound();

        // Lấy ngẫu nhiên 3 category khác
        var otherCategories = _context.Categories
            .Where(c => c.CategoryId != id)
            .OrderBy(r => Guid.NewGuid())
            .Take(3)
            .ToList();

        ViewBag.OtherCategories = otherCategories;
        return View(category);
    }

    [HttpGet]
    public IActionResult OrderDetail(int id)
    {
        var userId = GetCurrentUserId();

        // 🔍 Lấy thông tin order + các món ăn liên quan
        var orderdt = _context.Orders
            .Include(o => o.OrderStatus)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Address)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItemSize)
                    .ThenInclude(ms => ms.MenuItem)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Combo)
            .FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

        if (orderdt == null)
            return NotFound();

        return View(orderdt);
    }

    private int GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (int.TryParse(claim?.Value, out var userId))
                return userId;
        }
        return 0;
    }
}
