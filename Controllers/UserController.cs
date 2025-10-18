using FoodOrderWeb.Models;
using FoodOrderWeb.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

[Authorize]
public class UserController : Controller
{
    private readonly FoodorderwebContext _context;

    public UserController(FoodorderwebContext context)
    {
        _context = context;
    }
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirst("UserId").Value);
    }

    // Trang panel chính
    public IActionResult Panel()
    {
        int userId = GetCurrentUserId();

        if (userId == null)
            return RedirectToAction("Index", "Login");

        // Lấy ID status "Completed"
        var completedStatusId = _context.OrderStatuses
                                        .Where(s => s.Name == "Completed")
                                        .Select(s => s.OrderStatusId)
                                        .FirstOrDefault();

        var model = new UserViewModel
        {
            // Tổng đơn hàng của user
            TotalOrders = _context.Orders.Count(o => o.UserId == userId),

            // Tổng địa chỉ giao hàng
            TotalAddresses = _context.Addresses.Count(a => a.UserId == userId),

            // Hiện tại Vouchers không có cột UserId, nên chỉ đếm tổng số voucher hệ thống
            TotalVouchers = _context.Vouchers.Count(),

            // Tổng chi tiêu chỉ tính các đơn Completed
            TotalSpent = _context.Orders
                                 .Where(o => o.UserId == userId && o.OrderStatusId == completedStatusId)
                                 .Sum(o => (decimal?)o.TotalAmount) ?? 0,
        };

        return View(model);
    }


    // Update từng thông tin cá nhân
    [HttpPost]
    public IActionResult UpdateField(string field, string value)
    {
        int userId = GetCurrentUserId();

        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
        if (user == null) return NotFound();

        switch (field)
        {
            case "FullName":
                user.FullName = value;
                break;
            case "Email":
                user.Email = value;
                break;
            case "PhoneNumber":
                user.Phone = value;
                break;
        }

        _context.SaveChanges();
        return RedirectToAction("Panel");
    }

    // GET: Form đổi mật khẩu (trong modal)
    public IActionResult ChangePassword()
    {
        return View();
    }

    // POST: Xử lý đổi mật khẩu
    [HttpPost]
    public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        int userId = GetCurrentUserId();

        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        var passwordHasher = new PasswordHasher<User>();

        // Check mật khẩu cũ
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword);
        if (result == PasswordVerificationResult.Failed)
        {
            TempData["Error"] = "Mật khẩu cũ không đúng!";
            return RedirectToAction("Panel");
        }

        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Mật khẩu nhập lại không khớp!";
            return RedirectToAction("Panel");
        }

        // Hash mật khẩu mới
        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);

        _context.SaveChanges();

        TempData["Success"] = "Đổi mật khẩu thành công!";
        return RedirectToAction("Panel");
    }

    public IActionResult Profile()
    {
        int userId = GetCurrentUserId();

        var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

        var model = new UserViewModel
        {
            user = user,
        };

        return View(model);
    }

    // Trang địa chỉ
    public IActionResult AddressPanel()
    {
        int userId = GetCurrentUserId();

        var model = new UserViewModel
        {
            Addresses = _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToList(),
            NewAddress = new AddAddressViewModel()
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult AddAddress(UserViewModel model)
    {
        int userId = GetCurrentUserId();

        // Lấy dữ liệu địa chỉ mới từ model lồng bên trong
        var newAddrModel = model.NewAddress;

        //// ✅ Kiểm tra hợp lệ model
        //if (!ModelState.IsValid)
        //{
        //    model.Addresses = _context.Addresses
        //        .Where(a => a.UserId == userId)
        //        .OrderByDescending(a => a.IsDefault)
        //        .ToList();

        //    return View("AddressPanel", model);
        //}

        // ✅ Kiểm tra trùng địa chỉ
        if (_context.Addresses.Any(a =>
            a.UserId == userId &&
            a.Street == newAddrModel.Street &&
            a.District == newAddrModel.District &&
            a.City == newAddrModel.City))
        {
            ModelState.AddModelError("NewAddress.Street", "Địa chỉ này đã tồn tại.");

            model.Addresses = _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToList();

            return View("AddressPanel", model);
        }

        // ✅ Nếu đặt làm mặc định thì bỏ mặc định cũ
        if (newAddrModel.IsDefault)
        {
            var oldDefault = _context.Addresses.FirstOrDefault(a => a.UserId == userId && a.IsDefault);
            if (oldDefault != null)
                oldDefault.IsDefault = false;
        }

        // ✅ Tạo đối tượng Address để lưu vào DB
        var newAddr = new Address
        {
            UserId = userId,
            Label = newAddrModel.Label,
            Street = newAddrModel.Street,
            City = newAddrModel.City,
            District = newAddrModel.District,
            IsDefault = newAddrModel.IsDefault,
            CreatedAt = DateTime.Now
        };

        _context.Addresses.Add(newAddr);
        _context.SaveChanges();

        TempData["Success"] = "✅ Thêm địa chỉ thành công!";
        return RedirectToAction("AddressPanel");
    }



    [HttpPost]
    public IActionResult DeleteAddress(int addressId)
    {
        int userId = GetCurrentUserId();
        var addr = _context.Addresses.FirstOrDefault(a => a.AddressId == addressId && a.UserId == userId);
        if (addr != null)
        {
            _context.Addresses.Remove(addr);
            _context.SaveChanges();
            TempData["Success"] = "Đã xóa địa chỉ.";
        }
        return RedirectToAction("AddressPanel");
    }

    [HttpPost]
    public IActionResult SetDefaultAddress(int addressId)
    {
        int userId = GetCurrentUserId();
        var all = _context.Addresses.Where(a => a.UserId == userId);
        foreach (var a in all) a.IsDefault = false;

        var target = all.FirstOrDefault(a => a.AddressId == addressId);
        if (target != null) target.IsDefault = true;

        _context.SaveChanges();
        TempData["Success"] = "Đã đặt làm địa chỉ mặc định.";
        return RedirectToAction("AddressPanel");
    }

    public IActionResult MyOrders()
    {
        int userId = GetCurrentUserId();

        // 🧾 Lấy danh sách đơn hàng của người dùng
        var orders = _context.Orders
            .Include(o => o.OrderStatus)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Address)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        return View(orders);
    }

    public IActionResult OrderDetails(int id)
    {
        int userId = GetCurrentUserId();

        var order = _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItemSize)
                    .ThenInclude(ms => ms.MenuItem)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Combo)
            .Include(o => o.OrderStatus)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Address)
            .FirstOrDefault(o => o.OrderId == id && o.UserId == userId);

        if (order == null)
            return NotFound();

        return View(order);
    }

}
