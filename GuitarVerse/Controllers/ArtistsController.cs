using GuitarVerse.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuitarVerse.Controllers
{
    public class ArtistsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ArtistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. СПИСЪК С ВСИЧКИ АРТИСТИ
        public async Task<IActionResult> Index()
        {
            var artists = await _context.Artists.ToListAsync();
            return View(artists);
        }

        // 2. ДЕТАЙЛНА СТРАНИЦА НА АРТИСТА
        public async Task<IActionResult> Details(int id)
        {
            var artist = await _context.Artists
                .Include(a => a.Products) // Зареждаме и неговите китари!
                .FirstOrDefaultAsync(a => a.ArtistID == id);

            if (artist == null) return NotFound();

            return View(artist);
        }
    }
}