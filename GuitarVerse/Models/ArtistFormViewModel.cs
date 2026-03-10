using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class ArtistFormViewModel
    {
        public Artist Artist { get; set; }

        // За качване на снимките
        public IFormFile? CardImageFile { get; set; } // Малката (за списъка)
        public IFormFile? HeroImageFile { get; set; } // Голямата (за детайлите)
    }
}