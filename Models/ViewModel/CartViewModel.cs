using FoodOrderWeb.Models.ViewModel;

namespace FoodOrderWeb.Models
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal Total { get; set; }
        public int CartCount { get; set; }
    }

}
