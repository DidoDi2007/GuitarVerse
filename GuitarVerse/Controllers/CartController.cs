using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GuitarVerse.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ПОМОЩЕН МЕТОД ЗА GUEST ID ---
        private string GetGuestId()
        {
            // Проверяваме дали вече имаме бисквитка "GuestID"
            if (Request.Cookies["GuestID"] == null)
            {
                // Ако няма, генерираме нов уникален код
                string newGuestId = Guid.NewGuid().ToString();

                // Записваме го в бисквитките на браузъра (валиден 30 дни)
                Response.Cookies.Append("GuestID", newGuestId, new CookieOptions { Expires = DateTime.Now.AddDays(30) });

                return newGuestId;
            }

            // Ако има, връщаме го
            return Request.Cookies["GuestID"];
        }

        // 1. ПОКАЗВАНЕ НА КОЛИЧКАТА
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            string guestId = GetGuestId(); // Взимаме и Guest ID-то

            // Логика: Търсим продукти, които са или на този UserID, или на този GuestID
            // (Така дори да се логне по-късно, може да обединим количките, но засега просто показваме)

            IQueryable<CartItem> query = _context.CartItems.Include(c => c.Product);

            if (userId != null)
            {
                // Ако е логнат
                query = query.Where(c => c.UserID == userId);
            }
            else
            {
                // Ако е гост
                query = query.Where(c => c.GuestID == guestId);
            }

            var cartItems = await query.ToListAsync();

            var viewModel = new CartViewModel
            {
                CartItems = cartItems
            };

            return View(viewModel);
        }

        // 2. ДОБАВЯНЕ В КОЛИЧКАТА (ВЕЧЕ РАБОТИ ЗА ГОСТИ)
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            string guestId = GetGuestId(); // Винаги взимаме GuestID, дори да не е логнат

            CartItem existingItem = null;

            if (userId != null)
            {
                // Логнат потребител
                existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId);
            }
            else
            {
                // Гост потребител
                existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.GuestID == guestId && c.ProductID == productId);
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductID = productId,
                    Quantity = quantity,
                    DateAdded = DateTime.Now,
                    // Записваме кое ID ползваме
                    UserID = userId,
                    GuestID = (userId == null) ? guestId : null // Ако не е логнат, пазим GuestID
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            // Пренасочваме към количката
            return RedirectToAction("Index");
        }

        // 3. ПРЕМАХВАНЕ
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        // 4. ПРОМЯНА НА БРОЙКАТА
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int change)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                item.Quantity += change;
                if (item.Quantity < 1) item.Quantity = 1;

                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            string guestId = GetGuestId();

            IQueryable<CartItem> query = _context.CartItems;

            if (userId != null)
            {
                query = query.Where(c => c.UserID == userId);
            }
            else
            {
                query = query.Where(c => c.GuestID == guestId);
            }

            // Сумираме количествата (напр. 2 китари + 1 усилвател = 3)
            int count = await query.SumAsync(c => c.Quantity);

            return Json(count);
        }
    }
}