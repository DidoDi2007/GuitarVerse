using GuitarVerse.Data;
using GuitarVerse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json; // Инсталирай Newtonsoft.Json през NuGet, ако го нямаш

namespace GuitarVerse.Controllers
{
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CompareSessionKey = "CompareList";

        public CompareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. ПОКАЗВАНЕ НА ТАБЛИЦАТА
        public async Task<IActionResult> Index()
        {
            var compareList = GetCompareList();
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => compareList.Contains(p.ProductID))
                .ToListAsync();

            return View(products);
        }

        // 2. ДОБАВЯНЕ В СРАВНЕНИЕ (AJAX)
        [HttpPost]
        public IActionResult Add(int id)
        {
            var compareList = GetCompareList();

            if (compareList.Count >= 4) // Лимит до 4 продукта
                return Json(new { success = false, message = "You can only compare up to 4 products." });

            if (!compareList.Contains(id))
            {
                compareList.Add(id);
                SaveCompareList(compareList);
            }

            return Json(new { success = true, count = compareList.Count });
        }

        // 3. ПРЕМАХВАНЕ
        public IActionResult Remove(int id)
        {
            var compareList = GetCompareList();
            compareList.Remove(id);
            SaveCompareList(compareList);
            return RedirectToAction("Index");
        }

        // ПОМОЩНИ МЕТОДИ ЗА СЕСИЯТА
        private List<int> GetCompareList()
        {
            var sessionData = HttpContext.Session.GetString(CompareSessionKey);
            return sessionData == null ? new List<int>() : JsonConvert.DeserializeObject<List<int>>(sessionData);
        }

        private void SaveCompareList(List<int> list)
        {
            HttpContext.Session.SetString(CompareSessionKey, JsonConvert.SerializeObject(list));
        }

        // 4. ВЗИМАНЕ НА ДАННИ ЗА ЛЕНТАТА (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetCompareData()
        {
            var compareList = GetCompareList();

            // Взимаме само ID, Име и Снимка, за да е лека заявката
            var products = await _context.Products
                .Where(p => compareList.Contains(p.ProductID))
                .Select(p => new { p.ProductID, p.Name, p.ImagePath })
                .ToListAsync();

            return Json(products);
        }

        // 5. ИЗЧИСТВАНЕ НА ВСИЧКО
        [HttpPost]
        public IActionResult ClearAll()
        {
            SaveCompareList(new List<int>()); // Записваме празен списък

            // Ако е AJAX (от малкото прозорче долу вдясно)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }

            // Ако е нормално кликване (от големия бутон Clear All в страницата)
            return RedirectToAction("Index");
        }

        // 6. ПРЕМАХВАНЕ НА ЕДИН ПРОДУКТ (AJAX)
        [HttpPost]
        public IActionResult RemoveItem(int id)
        {
            var compareList = GetCompareList();
            if (compareList.Contains(id))
            {
                compareList.Remove(id);
                SaveCompareList(compareList);
            }
            return Json(new { success = true });
        }
    }
}