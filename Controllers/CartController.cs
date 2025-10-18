using FoodOrderWeb.Models;
using FoodOrderWeb.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace FoodOrderWeb.Controllers
{
    public class CartController : Controller
    {
        private readonly FoodorderwebContext _context;

        public CartController(FoodorderwebContext context)
        {
            _context = context;
        }
        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst("UserId").Value);
        }

        // 🧩 Lấy giỏ hàng từ Session
        private List<CartItemViewModel> GetCart()
        {
            var sessionData = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionData))
                return new List<CartItemViewModel>();

            return JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionData);
        }

        // 💾 Lưu giỏ hàng vào Session
        private void SaveCart(List<CartItemViewModel> cart)
        {
            var jsonData = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString("Cart", jsonData);
        }

        //gio hang chinh
        public IActionResult Index()
        {
            var cart = GetCart();

            // Tính tổng tiền
            foreach (var item in cart)
            {
                item.TotalPrice = item.UnitPrice * item.Quantity;
            }

            var model = new CartViewModel
            {
                Items = cart,
                Total = cart.Sum(x => x.TotalPrice),
                CartCount = cart.Sum(x => x.Quantity)
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, bool isCombo, int? menuItemSizeId, int quantity = 1)
        {
            var cart = GetCart();

            // 🧩 Tạo biến để lưu item mới
            CartItemViewModel newItem;

            if (isCombo)
            {
                var combo = _context.Combos.FirstOrDefault(c => c.ComboId == productId);
                if (combo == null)
                    return Json(new { success = false, message = "Combo không tồn tại" });

                newItem = new CartItemViewModel
                {
                    ProductId = combo.ComboId,
                    ProductName = combo.Name,
                    UnitPrice = combo.Price,
                    Quantity = quantity,
                    IsCombo = true
                };

                newItem.TotalPrice = newItem.UnitPrice * newItem.Quantity;
            }
            else
            {
                var menuItem = _context.MenuItems
                    .Include(m => m.MenuItemSizes)
                    .FirstOrDefault(m => m.MenuItemId == productId);

                if (menuItem == null)
                    return Json(new { success = false, message = "Món ăn không tồn tại" });

                decimal unitPrice = menuItem.Price;
                string sizeName = null;

                // Nếu có size, dùng giá theo size
                if (menuItemSizeId.HasValue)
                {
                    var size = menuItem.MenuItemSizes.FirstOrDefault(s => s.MenuItemSizeId == menuItemSizeId);
                    if (size != null)
                    {
                        unitPrice = size.Price;
                        sizeName = size.SizeName;
                    }
                }

                newItem = new CartItemViewModel
                {
                    ProductId = menuItem.MenuItemId,
                    ProductName = menuItem.Name + (sizeName != null ? $" ({sizeName})" : ""),
                    ImageUrl = menuItem.ImageUrl,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    IsCombo = false,
                    MenuItemSizeId = menuItemSizeId
                };

                newItem.TotalPrice = newItem.UnitPrice * newItem.Quantity;
            }

            // ✅ Nếu trong giỏ đã có item đó thì tăng số lượng
            var existingItem = cart.FirstOrDefault(x =>
                x.ProductId == newItem.ProductId &&
                x.IsCombo == newItem.IsCombo &&
                x.MenuItemSizeId == newItem.MenuItemSizeId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;

                existingItem.TotalPrice = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                cart.Add(newItem);
            }

            SaveCart(cart);

            // 🔢 Đếm lại tổng số lượng món
            int totalCount = cart.Sum(x => x.Quantity);

            return Json(new { success = true, count = totalCount });
        }

        // ❌ Xoá 1 sản phẩm khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveItem(int productId, bool isCombo, int? menuItemSizeId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.IsCombo == isCombo &&
                x.MenuItemSizeId == menuItemSizeId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return Json(new { success = true });
        }

        // 🔄 Cập nhật số lượng sản phẩm
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, bool isCombo, int? menuItemSizeId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.IsCombo == isCombo &&
                x.MenuItemSizeId == menuItemSizeId);

            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                SaveCart(cart);
            }

            return Json(new { success = true });
        }

        // 🚮 Xoá toàn bộ giỏ hàng
        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return Json(new { success = true });
        }

        // 🛒 Lấy số lượng món trong giỏ hàng (dùng cho hiển thị icon giỏ hàng)
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            int count = cart.Sum(x => x.Quantity);
            return Json(new { count });
        }

        [HttpPost]
        public IActionResult AddAddress([Bind(Prefix = "NewAddress")] AddAddressViewModel model)
        {
            int userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                var viewModel = new UserViewModel
                {
                    Addresses = _context.Addresses
                        .Where(a => a.UserId == userId)
                        .OrderByDescending(a => a.IsDefault)
                        .ToList(),
                    NewAddress = model
                };

                return Json(new
                {
                    success = true,
                    message = "Thêm địa chỉ thất bại!",
                    address = new
                    {
                        id = model.AddressId,
                        text = $"{model.Street}, {model.District}, {model.City}"
                    }
                });
            }

            if (model.IsDefault)
            {
                var oldDefault = _context.Addresses
                    .FirstOrDefault(a => a.UserId == userId && a.IsDefault);
                if (oldDefault != null)
                    oldDefault.IsDefault = false;
            }

            var newAddr = new Address
            {
                UserId = userId,
                Label = model.Label,
                Street = model.Street,
                City = model.City,
                District = model.District,
                IsDefault = model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(newAddr);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                message = "Thêm địa chỉ thành công!",
                address = new
                {
                    id = model.AddressId,
                    text = $"{model.Street}, {model.District}, {model.City}"
                }
            });
        }
    }
}
