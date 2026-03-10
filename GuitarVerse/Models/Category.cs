using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        // Връзка: Една категория има много продукти
        public ICollection<Product> Products { get; set; }
    }
}