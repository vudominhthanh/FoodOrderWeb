using FoodOrderWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderWeb.Components
{
    public class CategoryMenu : ViewComponent
    {
        private readonly FoodorderwebContext _context;

        public CategoryMenu(FoodorderwebContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }
    }
}
