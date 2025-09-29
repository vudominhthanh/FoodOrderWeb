namespace FoodOrderWeb.Models.ViewModel
{
    public class CategoryForHomeViewModel
    {
            public Category? Category { get; set; }
            public List<Category>? Categories { get; set; }
            public List<MenuItem>? MenuItems { get; set; }
            public List<Voucher>? Vouchers { get; set; }
            public List<Combo>? Combos { get; set; }
    }
}
