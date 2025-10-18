namespace FoodOrderWeb.Models.ViewModel
{
    public class CartItemViewModel
    {
        // 🔹 Id sản phẩm: là MenuItemId hoặc ComboId
        public int ProductId { get; set; }

        // 🔹 Nếu món có size (null nếu là combo hoặc món không có size)
        public int? MenuItemSizeId { get; set; }

        // 🔹 Xác định loại sản phẩm
        public bool IsCombo { get; set; }

        // 🔹 Thông tin hiển thị
        public string ProductName { get; set; } = string.Empty;
        public string? SizeName { get; set; }
        public string? ImageUrl { get; set; }

        // 🔹 Giá và số lượng
        public decimal UnitPrice { get; set; }  
        public int Quantity { get; set; }

        // 🔹 Tính tổng từng dòng
        public decimal TotalPrice { get; set; }

        // 🔹 (Tuỳ chọn) Ghi chú riêng cho từng món
        public string? Note { get; set; }
    }
}
