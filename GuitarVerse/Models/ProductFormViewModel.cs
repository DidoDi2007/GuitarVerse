using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class ProductFormViewModel
    {
        public Product Product { get; set; }
        public IEnumerable<Category> Categories { get; set; }

        // Главната снимка
        public IFormFile? ImageFile { get; set; }

        // НОВО: Списък за галерията
        public List<IFormFile>? GalleryFiles { get; set; }
        public IEnumerable<Artist>? Artists { get; set; }
    }
}