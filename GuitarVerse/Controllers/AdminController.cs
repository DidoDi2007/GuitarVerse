using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuitarVerse.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // <--- НОВО

        public AdminController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // --- ПРОВЕРКА ЗА АДМИН (извикваме я във всеки метод) ---
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "admin"; // Трябва да съвпада с това в базата (малки букви)
        }

        // 1. DASHBOARD (Таблото)
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Събираме статистика
            var model = new AdminDashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(),
                // Броим само платените, които още не са изпратени
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Paid"),
                TotalSales = await _context.Orders.Where(o => o.Status != "Pending").SumAsync(o => o.TotalAmount),

                // Взимаме последните 5 поръчки
                RecentOrders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        // 2. СПИСЪК С ПРОДУКТИ
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductID)
                .ToListAsync();

            return View(products);
        }

        // 3. СЪЗДАВАНЕ (Формата)
        [HttpGet]
        public IActionResult CreateProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var model = new ProductFormViewModel
            {
                Product = new Product(),
                Categories = _context.Categories.ToList()
            };

            return View(model);
        }

        // 4. ЗАПИСВАНЕ (Логиката с качване на снимка)
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductFormViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // --- ВАЖНО: ИГНОРИРАМЕ НЕНУЖНИТЕ ПРОВЕРКИ ---

            // 1. Игнорираме главната снимка (защото я качваме ръчно)
            ModelState.Remove("Product.ImagePath");

            // 2. Игнорираме навигационните свойства (те са null при създаване)
            ModelState.Remove("Product.Category");
            ModelState.Remove("Product.Reviews");  // <--- Това оправя "Reviews field is required"
            ModelState.Remove("Product.Images");   // <--- Това оправя "Images field is required"

            // 3. Игнорираме списъка с категории (той служи само за визуализация)
            ModelState.Remove("Categories");       // <--- Това оправя "Categories field is required"

            // --- НОВА ПРОВЕРКА ЗА ЛИМИТ НА СНИМКИТЕ ---
            if (model.GalleryFiles != null && model.GalleryFiles.Count > 5)
            {
                // Добавяме грешка към модела. Тя ще се покаже под полето за файлове.
                ModelState.AddModelError("GalleryFiles", "You can upload a maximum of 5 additional images.");
            }

            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;

                // 1. ЗАПИСВАНЕ НА ГЛАВНАТА СНИМКА
                if (model.ImageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    string path = Path.Combine(wwwRootPath + "/images/products/", fileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    model.Product.ImagePath = "/images/products/" + fileName;
                }
                else
                {
                    model.Product.ImagePath = "https://placehold.co/400";
                }

                // Първо записваме продукта, за да получим ID
                _context.Products.Add(model.Product);
                await _context.SaveChangesAsync();

                // 2. ЗАПИСВАНЕ НА ГАЛЕРИЯТА (НОВА ЛОГИКА)
                if (model.GalleryFiles != null && model.GalleryFiles.Count > 0)
                {
                    foreach (var file in model.GalleryFiles)
                    {
                        string galleryFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string galleryPath = Path.Combine(wwwRootPath + "/images/products/", galleryFileName);

                        using (var stream = new FileStream(galleryPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Създаваме запис в таблицата ProductImages
                        var productImage = new ProductImage
                        {
                            ProductID = model.Product.ProductID, // Вече имаме ID-то
                            ImagePath = "/images/products/" + galleryFileName
                        };
                        _context.ProductImages.Add(productImage);
                    }

                    // Записваме и картинките в базата
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Products");
            }

            // Ако все пак има грешка, презареждаме категориите
            model.Categories = _context.Categories.ToList();
            return View(model);
        }

        // 5. ИЗТРИВАНЕ
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Опционално: Тук може да изтриеш и файла на снимката от папката
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Products");
        }

        // ==========================
        // ORDERS MANAGEMENT
        // ==========================

        // 1. СПИСЪК С ПОРЪЧКИ
        public async Task<IActionResult> Orders(string status = "All")
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var query = _context.Orders.AsQueryable();

            // Филтър (ако искаш да видиш само платените)
            if (status == "Pending") query = query.Where(o => o.Status == "Pending");
            if (status == "Paid") query = query.Where(o => o.Status == "Paid");
            if (status == "Shipped") query = query.Where(o => o.Status == "Shipped");

            var orders = await query
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(orders);
        }

        // 2. ДЕТАЙЛИ ЗА ПОРЪЧКА
        public async Task<IActionResult> OrderDetails(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. ИЗПРАЩАНЕ НА ПРАТКА (SHIPPING)
        [HttpPost]
        public async Task<IActionResult> ShipOrder(int orderId, string trackingNumber)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.TrackingNumber = trackingNumber;
                order.Status = "Shipped"; // Сменяме статуса

                await _context.SaveChangesAsync();

                // Тук по желание можеш да пратиш имейл "Your order has been shipped!"
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // 4. МАРКИРАНЕ КАТО ДОСТАВЕНА (DELIVERED)
        [HttpPost]
        public async Task<IActionResult> MarkDelivered(int orderId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.Status == "Shipped")
            {
                order.Status = "Delivered"; // Краен статус
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

    }
}