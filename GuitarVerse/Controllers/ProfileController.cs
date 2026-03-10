using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GuitarVerse.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ПОМОЩЕН МЕТОД: Взима ID на логнатия ---
        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserID");
        }

        // 1. DASHBOARD (Трите карти)
        public IActionResult Index()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");
            return View();
        }

        // 2. MY ORDERS (История)
        public async Task<IActionResult> Orders()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserID == userId)
                .Where(o => o.Status != "Pending")
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 3. EDIT PROFILE (Лични данни)
        // GET: Edit
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            // Зареждаме и User, и Customer
            var user = await _context.Users.FindAsync(userId);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserID == userId);

            var model = new EditProfileViewModel
            {
                Email = user.Email // Зареждаме имейла
            };

            if (customer != null)
            {
                model.FirstName = customer.FirstName;
                model.LastName = customer.LastName;
                model.Phone = customer.Phone;
                model.Address = customer.Address;
                model.City = customer.City;
                model.Country = customer.Country;
            }

            return View(model);
        }

        // POST: Edit
        [HttpPost]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            // 1. Обновяваме Имейла (в Users)
            var user = await _context.Users.FindAsync(userId);

            // Проверка: Дали този имейл вече не се ползва от друг?
            if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserID != userId))
            {
                ModelState.AddModelError("Email", "This email is already taken.");
                return View(model);
            }
            user.Email = model.Email;

            // 2. Обновяваме данните (в Customers)
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserID == userId);
            if (customer == null)
            {
                customer = new Customer { UserID = userId.Value };
                _context.Customers.Add(customer);
            }

            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;
            customer.Phone = model.Phone;
            customer.Address = model.Address;
            customer.City = model.City;
            customer.Country = model.Country;

            await _context.SaveChangesAsync();

            ViewBag.Message = "Profile updated successfully!";
            return View(model);
        }

        // 4. SECURITY (Смяна на парола)
        [HttpGet]
        public IActionResult Security()
        {
            if (GetCurrentUserId() == null) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Security(ChangePasswordViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FindAsync(userId);

            // Проверка на старата парола
            var currentHash = HashPassword(model.CurrentPassword);
            if (user.PasswordHash != currentHash)
            {
                ModelState.AddModelError("CurrentPassword", "Incorrect current password.");
                return View(model);
            }

            // Запис на новата парола
            user.PasswordHash = HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Password changed successfully!";
            return View();
        }

        // Хеширане (същото като в AccountController)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        // POST: /Profile/DeleteAccount
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                // Изтриваме потребителя. 
                // SQL ще изтрие автоматично: Reviews, CartItems, Customers, SupportMessages
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            // Изчистваме сесията (Logout)
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}