using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GuitarVerse.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            int? categoryId,
             string subType,
            string[] selectedBrands,
            string query,
            string sortOrder,
            decimal? minPrice, decimal? maxPrice,
            string orientation, int? strings, string bridge, string pickup)
        {
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // 1. Филтриране
            if (categoryId.HasValue)
                productsQuery = productsQuery.Where(p => p.CategoryID == categoryId.Value);

            // --- НОВА ФИЛТРАЦИЯ ---
            if (!string.IsNullOrEmpty(subType))
            {
                productsQuery = productsQuery.Where(p => p.SubType == subType);
            }
            // ----------------------

            if (selectedBrands != null && selectedBrands.Length > 0)
            {
                productsQuery = productsQuery.Where(p => selectedBrands.Contains(p.Brand));
            }

            if (!string.IsNullOrEmpty(query))
                productsQuery = productsQuery.Where(p => p.Name.Contains(query) || p.Brand.Contains(query));

            if (minPrice.HasValue) productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrEmpty(orientation)) productsQuery = productsQuery.Where(p => p.Orientation == orientation);
            if (strings.HasValue) productsQuery = productsQuery.Where(p => p.NumberOfStrings == strings.Value);
            if (!string.IsNullOrEmpty(bridge)) productsQuery = productsQuery.Where(p => p.BridgeType == bridge);
            if (!string.IsNullOrEmpty(pickup)) productsQuery = productsQuery.Where(p => p.PickupType == pickup);

            // 2. Сортиране
            switch (sortOrder)
            {
                case "price_asc": productsQuery = productsQuery.OrderBy(p => p.Price); break;
                case "price_desc": productsQuery = productsQuery.OrderByDescending(p => p.Price); break;
                default: productsQuery = productsQuery.OrderByDescending(p => p.ProductID); break;
            }

            var viewModel = new ShopViewModel
            {
                Products = await productsQuery.ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                Brands = await _context.Products.Select(p => p.Brand).Distinct().ToListAsync(),

                CurrentBrand = (selectedBrands != null && selectedBrands.Length == 1) ? selectedBrands[0] : null,

                CurrentCategoryId = categoryId,
                SelectedBrands = selectedBrands != null ? selectedBrands.ToList() : new List<string>(),
                SearchQuery = query,
                SortOrder = sortOrder,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SelectedOrientation = orientation,
                SelectedStrings = strings,
                SelectedBridge = bridge,
                SelectedPickup = pickup,
                 SelectedSubType = subType
            };

            // 2. АКО Е AJAX ЗАЯВКА -> ВРЪЩАМЕ JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var jsonResult = viewModel.Products.Select(p => new
                {
                    p.ProductID, // <--- ВАЖНО: Добавих това, за да работят линковете!
                    p.ImagePath,
                    p.Brand,
                    p.Name,
                    p.Price,
                    p.Stock
                });

                return Json(jsonResult);
            }

            // 3. Ако е нормално зареждане -> връщаме цялата страница
            return View(viewModel);
        }

        // --- НОВИЯТ МЕТОД ЗА ДЕТАЙЛИ ---
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)        // <--- НОВО: Зареждаме ревютата
                    .ThenInclude(r => r.User)   // <--- НОВО: Зареждаме и потребителите към тях
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: /Shop/AddReview
        [HttpPost]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null) return RedirectToAction("Login", "Account");

            var review = new Review
            {
                ProductID = productId,
                UserID = userId.Value,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Връщаме потребителя обратно на същата страница
            return RedirectToAction("Details", new { id = productId });
        }
    }
}