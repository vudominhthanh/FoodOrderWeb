using FoodOrderWeb.Models;
using FoodOrderWeb.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;

[Authorize]
public class OrderController : Controller
{
    private readonly FoodorderwebContext _context;

    public OrderController(FoodorderwebContext context)
    {
        _context = context;
    }

    // 🧩 Lấy giỏ hàng từ Session
    private List<CartItemViewModel> GetCartFromSession()
    {
        var sessionData = HttpContext.Session.GetString("Cart");
        if (string.IsNullOrEmpty(sessionData))
            return new List<CartItemViewModel>();

        return JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionData);
    }    
    
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("UserId").Value);
    }


    [HttpGet]
    public IActionResult Checkout()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return RedirectToAction("Login", "Account");

        // 🛒 Lấy giỏ hàng từ session
        var sessionData = HttpContext.Session.GetString("Cart");
        var cart = string.IsNullOrEmpty(sessionData)
            ? new List<CartItemViewModel>()
            : JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionData);

        // 📦 Lấy user và danh sách địa chỉ
        var user = _context.Users
            .Include(u => u.Addresses)
            .FirstOrDefault(u => u.UserId == userId);

        // 💰 Tổng tiền
        decimal subtotal = cart.Sum(c => c.TotalPrice);

        // 💳 Phương thức thanh toán
        var paymentMethods = _context.PaymentMethods
            .Select(pm => pm.Name)
            .ToList();

        // 📍 Lấy địa chỉ mặc định (nếu có)
        var defaultAddress = user?.Addresses.FirstOrDefault(a => a.IsDefault);

        // ✅ Tạo model
        var model = new OrderViewModel
        {
            UserId = userId,
            FullName = user?.FullName,
            Phone = user?.Phone,
            AvailableAddresses = user?.Addresses.ToList() ?? new List<Address>(),
            SelectedAddressId = defaultAddress?.AddressId,  // lưu id để dùng khi đặt hàng
            Address = defaultAddress != null
                ? $"{defaultAddress.Street}, {defaultAddress.Label}, {defaultAddress.District}, {defaultAddress.City}"
                : null,  // 👈 thêm dòng này để View hiển thị luôn địa chỉ
            CartItems = cart,
            Subtotal = subtotal,
            PaymentMethods = paymentMethods,
            PaymentMethod = paymentMethods.FirstOrDefault() ?? "COD"
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Checkout(OrderViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return RedirectToAction("Login", "User");

        List<CartItemViewModel> cartItems;

        // ✅ 1️⃣ Kiểm tra xem có phải thanh toán nhanh 1 món (Details) không
        if (Request.Form["IsSingleCheckout"] == "true")
        {
            // 🟢 Thanh toán 1 món (từ trang Details)
            cartItems = new List<CartItemViewModel>
        {
            new CartItemViewModel
            {
                ProductId = int.Parse(Request.Form["ProductId"]),
                Quantity = int.Parse(Request.Form["Quantity"]),
                IsCombo = bool.Parse(Request.Form["IsCombo"]),
                UnitPrice = model.Subtotal, // giá được truyền từ form
                ProductName = "Thanh toán nhanh"
            }
        };
        }
        else
        {
            // 🟡 Thanh toán từ giỏ hàng (Cart)
            var sessionData = HttpContext.Session.GetString("Cart");
            cartItems = string.IsNullOrEmpty(sessionData)
                ? new List<CartItemViewModel>()
                : JsonConvert.DeserializeObject<List<CartItemViewModel>>(sessionData);

            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction("Checkout");
            }
        }

        // 🏠 Lấy địa chỉ mặc định
        var defaultAddress = _context.Addresses
            .FirstOrDefault(a => a.UserId == userId && a.IsDefault);

        if (defaultAddress == null)
        {
            TempData["Error"] = "⚠️ Bạn chưa có địa chỉ mặc định.";
            return RedirectToAction("AddressPanel", "User");
        }

        // 🎟️ Tính giảm giá
        decimal discount = 0;
        if (!string.IsNullOrEmpty(model.VoucherCode))
        {
            var voucher = _context.Vouchers.FirstOrDefault(v =>
                v.Code == model.VoucherCode && v.IsActive &&
                v.StartDate <= DateTime.UtcNow && v.EndDate >= DateTime.UtcNow);

            if (voucher != null)
            {
                discount = voucher.DiscountPercent.HasValue
                    ? cartItems.Sum(c => c.TotalPrice) * (voucher.DiscountPercent.Value / 100)
                    : voucher.DiscountAmount ?? 0;
            }
        }

        // ⚙️ Trạng thái & phương thức thanh toán
        var orderStatus = _context.OrderStatuses
                .FirstOrDefault(s => s.Name.Trim().ToLower() == "pending");

        if (orderStatus == null)
        {
            TempData["Error"] = "⚠️ Hệ thống chưa có trạng thái 'Pending'.";
            return RedirectToAction("Index", "Cart");
        }

        var paymentMethod = _context.PaymentMethods.FirstOrDefault(pm => pm.Name == model.PaymentMethod);
        if (paymentMethod == null)
        {
            TempData["Error"] = $"⚠️ Phương thức thanh toán '{model.PaymentMethod}' không hợp lệ.";
            return RedirectToAction("Index", "Cart");
        }

        // 💰 Tổng tạm tính
        decimal subtotal = cartItems.Sum(i => i.UnitPrice * i.Quantity);
        decimal total = subtotal - discount + model.ShippingFee;

        // 🧾 Tạo đơn hàng
        var order = new Order
        {
            UserId = userId,
            AddressId = defaultAddress.AddressId,
            OrderDate = DateTime.Now,
            OrderStatusId = orderStatus.OrderStatusId,
            PaymentMethodId = paymentMethod.PaymentMethodId,
            TotalAmount = total,
            ShippingFee = model.ShippingFee,
            VoucherCode = model.VoucherCode,
            Note = model.Note,
            Paid = false,
            CreatedAt = DateTime.Now
        };

        _context.Orders.Add(order);
        _context.SaveChanges(); // để có OrderId

        // 🍱 Thêm từng món
        foreach (var item in cartItems)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.OrderId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ComboId = item.IsCombo ? item.ProductId : null,
                MenuItemSizeId = !item.IsCombo ? item.MenuItemSizeId : null
            };
            _context.OrderItems.Add(orderItem);
        }

        _context.SaveChanges();

        // 🧹 Dọn session giỏ hàng nếu là thanh toán từ Cart
        if (Request.Form["IsSingleCheckout"] != "true")
            HttpContext.Session.Remove("Cart");

        TempData["Success"] = "✅ Đặt hàng thành công!";

        // 🏁 Dẫn về trang lịch sử đơn hàng
        return RedirectToAction("MyOrders", "User");
    }




    // Thanh toán nhanh cho 1 món
    [HttpGet]
    public IActionResult CheckoutSingle(int productId, bool isCombo, int? menuItemSizeId, int quantity = 1, string? voucherCode = null)
    {
        if (quantity <= 0) quantity = 1;

        var cartItems = new List<CartItemViewModel>();

        if (isCombo)
        {
            var combo = _context.Combos.FirstOrDefault(c => c.ComboId == productId);
            if (combo == null)
                return RedirectToAction("Index", "Home");

            cartItems.Add(new CartItemViewModel
            {
                ProductId = combo.ComboId,
                ProductName = combo.Name,
                UnitPrice = combo.Price,
                Quantity = quantity,
                IsCombo = true,
                TotalPrice = combo.Price * quantity,
            });
        }
        else
        {
            var size = _context.MenuItemSizes
                .Include(s => s.MenuItem)
                .FirstOrDefault(s => s.MenuItemSizeId == menuItemSizeId);

            if (size == null)
                return RedirectToAction("Index", "Home");

            cartItems.Add(new CartItemViewModel
            {
                ProductId = size.MenuItemId,
                ProductName = $"{size.MenuItem.Name} ({size.SizeName})",
                UnitPrice = size.Price,
                Quantity = quantity,
                SizeName = size.SizeName,
                IsCombo = false,
                MenuItemSizeId = size.MenuItemSizeId,
                TotalPrice = size.Price * quantity,
                ImageUrl = size.MenuItem.ImageUrl
            });
        }

        // 🧾 Tính tiền ban đầu
        decimal subtotal = cartItems.Sum(x => x.TotalPrice);
        decimal shippingFee = 15000m; // phí vận chuyển mặc định
        decimal discountDecimal = 0m;

        // 🎟️ Nếu có mã giảm giá -> thiết lập vào model theo cách model chấp nhận
        int? voucherId = null;
        decimal? discountPercent = null;
        decimal? discountAmount = null;

        if (!string.IsNullOrEmpty(voucherCode))
        {
            var voucher = _context.Vouchers.FirstOrDefault(v =>
                v.Code == voucherCode && v.IsActive &&
                v.StartDate <= DateTime.UtcNow && v.EndDate >= DateTime.UtcNow);

            if (voucher != null)
            {
                voucherId = voucher.VoucherId; // nếu cần lưu
                if (voucher.DiscountPercent.HasValue)
                {
                    discountPercent = voucher.DiscountPercent.Value;
                    discountDecimal = subtotal * (discountPercent.Value / 100m);
                }
                else if (voucher.DiscountAmount.HasValue)
                {
                    discountAmount = voucher.DiscountAmount.Value;
                    discountDecimal = discountAmount.Value;
                }
            }
        }

        // 💰 Tổng (tính local để dùng trước, model.Total cũng có thể được dùng sau khi set Subtotal/Shipping/DiscountPercent/Amount)
        decimal total = subtotal - discountDecimal + shippingFee;
        if (total < 0) total = 0;

        // Gán vào model bằng các property có setter (không gán vào Discount/Total trực tiếp)
        var model = new OrderViewModel
        {
            UserId = GetCurrentUserId(),
            CartItems = cartItems,
            Subtotal = subtotal,
            ShippingFee = shippingFee,
            VoucherId = voucherId,
            VoucherCode = voucherCode,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            // Note: Discount (read-only) will be computed from DiscountPercent/DiscountAmount,
            // Total (read-only) will be computed from Subtotal - Discount + ShippingFee.
        };
         
        // Nếu bạn cần truyền giá trị total lập tức (ví dụ view dùng input hidden có tên Total),
        // dùng ViewData / ViewBag hoặc một property tạm có setter trong model (không recommend).
        ViewBag.TotalPreview = total;

        return RedirectToAction("MyOrders", "User");
    }



    // ✅ Lấy danh sách phương thức thanh toán từ DB
    [HttpGet]
    public IActionResult GetPaymentMethods()
    {
        // Dùng FromSqlRaw vì chỉ cần bảng đơn giản
        var methods = _context.PaymentMethods
            .FromSqlRaw("SELECT PaymentMethodId, [Name] FROM PaymentMethod WHERE 1=1")
            .ToList();

        return Json(methods);
    }


    // ✅ Kiểm tra voucher hợp lệ với sản phẩm
    [HttpGet]
    public IActionResult ValidateVoucher(string code)
    {
        if (string.IsNullOrEmpty(code))
            return Json(new { success = false, message = "⚠️ Vui lòng nhập mã voucher." });

        // Lấy voucher cơ bản
        var voucher = _context.Vouchers
            .FirstOrDefault(v => v.Code == code && v.IsActive && v.StartDate <= DateTime.UtcNow && v.EndDate >= DateTime.UtcNow);

        if (voucher == null)
            return Json(new { success = false, message = "❌ Mã voucher không tồn tại hoặc đã hết hạn." });

        // Lấy giỏ hàng từ session
        var cartJson = HttpContext.Session.GetString("Cart");
        if (string.IsNullOrEmpty(cartJson))
            return Json(new { success = false, message = "🛒 Giỏ hàng trống." });

        var cartItems = JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartJson);

        bool applicable = false;

        foreach (var item in cartItems)
        {
            if (item.IsCombo)
            {
                // Truy vấn bảng join trực tiếp
                var comboMatch = _context.Database.SqlQueryRaw<int>(
                    @"SELECT COUNT(*) FROM VoucherCombos vc
                  INNER JOIN Combos c ON c.ComboId = vc.ComboId
                  WHERE vc.VoucherId = {0} AND c.ComboId = {1}",
                    voucher.VoucherId, item.ProductId
                ).AsEnumerable().FirstOrDefault();

                if (comboMatch > 0)
                {
                    applicable = true;
                    break;
                }
            }
            else if (item.MenuItemSizeId != null)
            {
                // Check theo size
                var sizeMatch = _context.Database.SqlQueryRaw<int>(
                    @"SELECT COUNT(*) FROM VoucherMenuItemSizes vms
                  INNER JOIN MenuItemSizes ms ON ms.MenuItemSizeId = vms.MenuItemSizeId
                  WHERE vms.VoucherId = {0} AND ms.MenuItemSizeId = {1}",
                    voucher.VoucherId, item.MenuItemSizeId
                ).AsEnumerable().FirstOrDefault();

                // Check theo món (nếu không có size)
                var itemMatch = _context.Database.SqlQueryRaw<int>(
                    @"SELECT COUNT(*) FROM VoucherMenuItems vmi
                  WHERE vmi.VoucherId = {0} AND vmi.MenuItemId = {1}",
                    voucher.VoucherId, item.ProductId
                ).AsEnumerable().FirstOrDefault();

                if (sizeMatch > 0 || itemMatch > 0)
                {
                    applicable = true;
                    break;
                }
            }
        }

        if (!applicable)
            return Json(new { success = false, message = "🚫 Voucher không áp dụng cho sản phẩm trong giỏ hàng." });

        var discountText = voucher.DiscountPercent.HasValue
            ? $"{voucher.DiscountPercent.Value}%"
            : $"{voucher.DiscountAmount.Value:N0}₫";

        return Json(new { success = true, message = $"✅ Áp dụng mã {voucher.Code} thành công! Giảm {discountText}." });
    }



    public IActionResult Success()
    {
        // Xóa giỏ hàng
        HttpContext.Session.Remove("Cart");
        return View();
    }

}
