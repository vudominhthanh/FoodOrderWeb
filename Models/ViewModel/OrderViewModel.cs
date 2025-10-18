using System;
using System.Collections.Generic;

namespace FoodOrderWeb.Models.ViewModel
{
    public class OrderViewModel
    {
        public int UserId { get; set; }

        // ✅ Thông tin khách hàng
        public string? FullName { get; set; }
        public string? Phone { get; set; }

        // ✅ Địa chỉ giao hàng
        public int? SelectedAddressId { get; set; }
        public string? Address { get; set; }
        public List<Address>? AvailableAddresses { get; set; }

        // ✅ Giỏ hàng
        public List<CartItemViewModel> CartItems { get; set; } = new();

        // ✅ Thanh toán
        public string? PaymentMethod { get; set; }
        public List<string>? PaymentMethods { get; set; }

        // ✅ Mã giảm giá
        public int? VoucherId { get; set; } // id thật trong DB
        public string? VoucherCode { get; set; } // mã nhập của người dùng
        public decimal? DiscountPercent { get; set; } // % giảm
        public decimal? DiscountAmount { get; set; } // số tiền giảm trực tiếp

        // ✅ Tổng tiền
        public decimal Subtotal { get; set; } // tổng giỏ hàng
        public decimal Discount
        {
            get
            {
                if (DiscountPercent.HasValue)
                    return Subtotal * (DiscountPercent.Value / 100);
                if (DiscountAmount.HasValue)
                    return DiscountAmount.Value;
                return 0;
            }
        }

        public decimal ShippingFee { get; set; } = 20000; // ví dụ 20k
        public decimal Total => Subtotal - Discount + ShippingFee;

        // ✅ Ghi chú đơn hàng
        public string? Note { get; set; }

        // ✅ Thời gian tạo (option)
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
