using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class Artist
    {
        public int ArtistID { get; set; }
        public string Name { get; set; }
        public string Band { get; set; }
        public string Bio { get; set; } // HTML текст
        public string CardImage { get; set; }
        public string HeroImage { get; set; }

        // Връзка към неговите продукти
        public ICollection<Product> Products { get; set; }
    }
}