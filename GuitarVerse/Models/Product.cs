namespace GuitarVerse.Models
{
    public class Product
    {
        public int ProductID { get; set; }
        public int CategoryID { get; set; }
        public Category Category { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int Stock { get; set; }
        public string? Brand { get; set; }
        public string? ImagePath { get; set; }

        // --- НОВИ ПОЛЕТА ---
        public string? Orientation { get; set; }  // Може и да махнеш default стойността
        public int? NumberOfStrings { get; set; } = 6;
        public string? BridgeType { get; set; } 
        public string? PickupType { get; set; }
        public string? SpecsText { get; set; } // <--- НОВО
        public string? SubType { get; set; } // <--- НОВО
        public int? ArtistID { get; set; }
        public Artist? Artist { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public string? Overview { get; set; } // HTML текст за описанието
        public ICollection<ProductImage> Images { get; set; } // Галерията
    }
}