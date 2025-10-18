namespace FoodOrderWeb.Models.ViewModel
{
    public class UserViewModel
    {
        // Thống kê
        public int? TotalOrders { get; set; }
        public int? TotalAddresses { get; set; }
        public int? TotalVouchers { get; set; }
        public decimal? TotalSpent { get; set; }

        // Thêm bảng
        public User user { get; set; }
        public OrderStatus OrderStatus { get; set; }

        // Địa chỉ
        public List<Address> Addresses { get; set; }
        public AddAddressViewModel NewAddress { get; set; } = new AddAddressViewModel();
    }
}
