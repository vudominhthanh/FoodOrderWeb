namespace FoodOrderWeb.Models.ViewModel
{
    public class CategoryViewModel
    {
        public Category? Category { get; set; }
        public List<MenuItem>? MenuItems { get; set; }
        public List<Voucher>? Vouchers { get; set; }
        public List<Combo>? Combos { get; set; }
    }
}
