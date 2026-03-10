using System.ComponentModel.DataAnnotations; // <--- 1. ДОБАВИ ТОВА ГОРЕ

namespace GuitarVerse.Models
{
    public class ProductImage
    {
        [Key] // <--- 2. ДОБАВИ ТОЗИ АТРИБУТ ТУК
        public int ImageID { get; set; }

        public int ProductID { get; set; }
        public Product Product { get; set; }

        public string ImagePath { get; set; }
    }
}