using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuitarVerse.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contact Page
        // GET: Contact Page
        public async Task<IActionResult> Contact()
        {
            var model = new SupportMessage();

            // 1. Проверяваме дали има логнат потребител
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId != null)
            {
                // 2. Взимаме Имейла от таблица Users
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    model.SenderEmail = user.Email;
                }

                // 3. Взимаме Името от таблица Customers (ако е попълнил профила си)
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserID == userId);
                if (customer != null)
                {
                    // Събираме Име + Фамилия
                    model.SenderName = $"{customer.FirstName} {customer.LastName}".Trim();
                }
            }

            // Подаваме пълния (или празен) модел на View-то
            return View(model);
        }

        // POST: Send Message
        [HttpPost]
        public async Task<IActionResult> Contact(SupportMessage model)
        {
            // Ако е логнат, записваме му ID-то
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId != null)
            {
                model.UserID = userId;
            }

            if (ModelState.IsValid)
            {
                _context.SupportMessages.Add(model);
                await _context.SaveChangesAsync();

                ViewBag.Success = "Your message has been sent! We will reply shortly.";
                ModelState.Clear(); // Чистим формата
                return View();
            }

            return View(model);
        }
    }
}