using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss; // <--- ТОВА Е БИБЛИОТЕКАТА

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

                // --- ЗАЩИТА: ПОЧИСТВАНЕ НА HTML (XSS) ---
                if (!string.IsNullOrEmpty(model.Product.Overview))
                {
                    var sanitizer = new HtmlSanitizer();
                    // Това маха <script> и оставя само безопасни тагове
                    model.Product.Overview = sanitizer.Sanitize(model.Product.Overview);
                }
                // ----------------------------------------


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

        // ==========================
        // EDIT PRODUCT LOGIC
        // ==========================

        // 1. ОТВАРЯНЕ НА ФОРМАТА ЗА РЕДАКЦИЯ
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var product = await _context.Products
                .Include(p => p.Images) // Зареждаме и галерията
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null) return NotFound();

            var model = new ProductFormViewModel
            {
                Product = product,
                Categories = _context.Categories.ToList()
            };

            return View(model);
        }

        // 2. ЗАПИСВАНЕ НА ПРОМЕНИТЕ
        [HttpPost]
        public async Task<IActionResult> EditProduct(int id, ProductFormViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Игнорираме валидацията за снимки (защото може да не искаш да ги сменяш)
            ModelState.Remove("Product.ImagePath");
            ModelState.Remove("Product.Category");
            ModelState.Remove("Product.Images");
            ModelState.Remove("Product.Reviews");
            ModelState.Remove("Categories");
            ModelState.Remove("ImageFile"); // При редакция не е задължително да качваш нова

            // --- НОВА ПРОВЕРКА ЗА ЛИМИТ (EDIT) ---
            if (model.GalleryFiles != null && model.GalleryFiles.Count > 0)
            {
                // 1. Преброяваме колко снимки има ТОЗИ продукт в момента в базата
                int existingCount = await _context.ProductImages.CountAsync(i => i.ProductID == id);

                // 2. Преброяваме колко нови се опитва да качи админът
                int newCount = model.GalleryFiles.Count;

                // 3. Ако сумата е над 5 -> ГРЕШКА
                if (existingCount + newCount > 5)
                {
                    ModelState.AddModelError("GalleryFiles", $"Limit exceeded! You already have {existingCount} images.");
                }
            }
            // -------------------------------------

            if (ModelState.IsValid)
            {
                // Намираме съществуващия продукт в базата
                var productToUpdate = await _context.Products.FindAsync(id);
                if (productToUpdate == null) return NotFound();

                // Обновяваме текстовите полета
                productToUpdate.Brand = model.Product.Brand;
                productToUpdate.Name = model.Product.Name;
                productToUpdate.CategoryID = model.Product.CategoryID;
                productToUpdate.Price = model.Product.Price;
                productToUpdate.Stock = model.Product.Stock;
                productToUpdate.NumberOfStrings = model.Product.NumberOfStrings;
                productToUpdate.Orientation = model.Product.Orientation;
                productToUpdate.PickupType = model.Product.PickupType;
                productToUpdate.BridgeType = model.Product.BridgeType;
                productToUpdate.Description = model.Product.Description;
                productToUpdate.Overview = model.Product.Overview;
                productToUpdate.SpecsText = model.Product.SpecsText;

                string wwwRootPath = _hostEnvironment.WebRootPath;

                // --- ЛОГИКА ЗА ГЛАВНАТА СНИМКА ---
                // Само ако има качен нов файл, сменяме старата снимка
                if (model.ImageFile != null)
                {
                    // (Опционално: Тук можеш да изтриеш стария файл от диска, за да не се трупат)

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                    string path = Path.Combine(wwwRootPath + "/images/products/", fileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    productToUpdate.ImagePath = "/images/products/" + fileName;
                }

                // --- ЛОГИКА ЗА ГАЛЕРИЯТА (ДОБАВЯНЕ НА ОЩЕ) ---
                if (model.GalleryFiles != null && model.GalleryFiles.Count > 0)
                {
                    foreach (var file in model.GalleryFiles)
                    {
                        string galleryName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string galleryPath = Path.Combine(wwwRootPath + "/images/products/", galleryName);

                        using (var stream = new FileStream(galleryPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var newImage = new ProductImage
                        {
                            ProductID = id,
                            ImagePath = "/images/products/" + galleryName
                        };
                        _context.ProductImages.Add(newImage);
                    }
                }

                // --- ЗАЩИТА ---
                if (!string.IsNullOrEmpty(model.Product.Overview))
                {
                    var sanitizer = new HtmlSanitizer();
                    productToUpdate.Overview = sanitizer.Sanitize(model.Product.Overview);
                }
                else
                {
                    productToUpdate.Overview = model.Product.Overview; // Ако е празно
                }
                // --------------


                _context.Products.Update(productToUpdate);
                await _context.SaveChangesAsync();

                return RedirectToAction("Products");
            }

            var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductID == id);

            model.Product = product; // Връщаме старите данни в модела
            model.Categories = _context.Categories.ToList();
            return View(model);
        }

        // 3. ИЗТРИВАНЕ НА ЕДНА СНИМКА ОТ ГАЛЕРИЯТА (AJAX или Form)
        [HttpPost]
        public async Task<IActionResult> DeleteGalleryImage(int imageId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var img = await _context.ProductImages.FindAsync(imageId);
            if (img != null)
            {
                // Запазваме ID на продукта, за да се върнем на същата страница
                int productId = img.ProductID;

                _context.ProductImages.Remove(img);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditProduct", new { id = productId });
            }
            return RedirectToAction("Products");
        }

        // ==========================
        // USER MANAGEMENT
        // ==========================

        // 1. СПИСЪК С ПОТРЕБИТЕЛИ
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt) // Най-новите най-горе
                .ToListAsync();

            return View(users);
        }

        // 2. ПРОМЯНА НА РОЛЯ (Admin <-> Customer)
        [HttpPost]
        public async Task<IActionResult> ToggleUserRole(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // ЗАЩИТА: Не позволяваме да променяш собствената си роля
            var currentUserId = HttpContext.Session.GetInt32("UserID");
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot change your own role!";
                return RedirectToAction("Users");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Ако е админ става клиент, ако е клиент става админ
                if (user.Role == "admin") user.Role = "customer";
                else user.Role = "admin";

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Users");
        }

        // 3. ИЗТРИВАНЕ НА ПОТРЕБИТЕЛ
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // ЗАЩИТА: Не позволяваме да изтриеш сам себе си
            var currentUserId = HttpContext.Session.GetInt32("UserID");
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account!";
                return RedirectToAction("Users");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Users");
        }



    }
}