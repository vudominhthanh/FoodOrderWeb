namespace FoodOrderWeb.Models.ViewModel
{
    public class FoodDetailsViewModel
    {
        public MenuItem MenuItem { get; set; }
        public Category Category { get; set; }
        public List<MenuItemSize> Sizes { get; set; }
        public List<Voucher> VouchersForFood { get; set; }
        public List<Voucher> VouchersForCategory { get; set; }
        public List<MenuItem> RelatedFoods { get; set; }
        public List<Combo> CombosWithFood { get; set; }
    }
}
