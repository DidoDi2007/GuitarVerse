using GuitarVerse.Data;
using GuitarVerse.Models;
using GuitarVerse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout; // Библиотеката на Stripe

namespace GuitarVerse.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService; // Добавяме EmailService

        public CheckoutController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Checkout
        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            // 1. Проверка за празна количка
            var cartItems = await GetCartItemsAsync();
            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel();

            // 2. АВТОМАТИЧНО ПОПЪЛВАНЕ (Magic!)
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId != null)
            {
                // Намираме данните на потребителя
                var user = await _context.Users.FindAsync(userId);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserID == userId);

                if (user != null)
                {
                    model.Email = user.Email; // Попълваме имейла
                }

                if (customer != null)
                {
                    // Попълваме адреса и имената, ако ги е запазил в профила си
                    model.FirstName = customer.FirstName;
                    model.LastName = customer.LastName;
                    model.Phone = customer.Phone;
                    model.Address = customer.Address;
                    model.City = customer.City;
                    model.Country = customer.Country;
                    // model.PostalCode = ... (Ако добавим пощенски код и в профила по-нататък)
                }
            }

            return View(model);
        }

        // POST: /Checkout/CreateStripeSession
        [HttpPost]
        public async Task<IActionResult> CreateStripeSession(CheckoutViewModel model)
        {
            if (!ModelState.IsValid) return View("Index", model);

            var cartItems = await GetCartItemsAsync();
            if (cartItems.Count == 0) return RedirectToAction("Index", "Cart");

            // 1. ЗАПИСВАМЕ ПОРЪЧКАТА В БАЗАТА (STATUS = PENDING)
            // Записваме я преди плащането, за да имаме ID
            var userId = HttpContext.Session.GetInt32("UserID");
            var guestId = Request.Cookies["GuestID"];

            // Използваме пълния път GuitarVerse.Models.Order
            var order = new GuitarVerse.Models.Order
            {
                UserID = userId,
                GuestID = (userId == null) ? guestId : null,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalAmount = cartItems.Sum(x => x.Quantity * x.Product.Price),

                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                PostalCode = model.PostalCode,
                Country = model.Country,

                // Тук също изрично посочваме
                OrderDetails = new List<GuitarVerse.Models.OrderDetail>()
            };

            // Прехвърляме продуктите
            foreach (var item in cartItems)
            {
                order.OrderDetails.Add(new GuitarVerse.Models.OrderDetail
                {
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    Price = item.Product.Price
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Вече имаме OrderID!

            // 2. СЪЗДАВАМЕ STRIPE SESSION (Плащането)
            var domain = "https://localhost:7124"; // ВАЖНО: Смени с твоя порт (виж в браузъра)

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"/Checkout/Success?orderId={order.OrderID}", // Връщаме се тук при успех
                CancelUrl = domain + "/Cart", // Връщаме се тук при отказ
                CustomerEmail = model.Email, // Stripe ще прати и техен имейл
            };

            foreach (var item in cartItems)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Product.Price * 100), // Stripe работи в стотинки/центове
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{item.Product.Brand} {item.Product.Name}",
                        },
                    },
                    Quantity = item.Quantity,
                });
            }

            var service = new SessionService();
            Session session = service.Create(options);

            // 3. ПРЕПРАЩАМЕ КЛИЕНТА КЪМ STRIPE
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // SUCCESS PAGE (След плащане)
        public async Task<IActionResult> Success(int orderId)
        {
            // Намираме поръчката
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null) return NotFound();

            // Ако вече е платена, просто показваме view-то (да не пращаме имейли 2 пъти при рефреш)
            if (order.Status == "Paid")
            {
                return View(orderId);
            }

            // 1. МАРКИРАМЕ КАТО ПЛАТЕНА
            order.Status = "Paid";

            // Намаляваме наличностите
            foreach (var detail in order.OrderDetails)
            {
                detail.Product.Stock -= detail.Quantity;
            }

            // 2. ИЗЧИСТВАМЕ КОЛИЧКАТА
            var cartItems = await GetCartItemsAsync();
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            // 3. ПРАЩАМЕ ИМЕЙЛ
            await _emailService.SendOrderConfirmationAsync(order.Email, order);

            return View(orderId);
        }

        private async Task<List<CartItem>> GetCartItemsAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var guestId = Request.Cookies["GuestID"];
            IQueryable<CartItem> query = _context.CartItems.Include(c => c.Product);
            if (userId != null) query = query.Where(c => c.UserID == userId);
            else query = query.Where(c => c.GuestID == guestId);
            return await query.ToListAsync();
        }
    }
}