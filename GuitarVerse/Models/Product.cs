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
        public string Orientation { get; set; } = "Right-Handed";
        public int NumberOfStrings { get; set; } = 6;
        public string BridgeType { get; set; } = "Fixed";
        public string PickupType { get; set; } = "Passive";
        public ICollection<Review> Reviews { get; set; }
        public string? Overview { get; set; } // HTML текст за описанието
        public ICollection<ProductImage> Images { get; set; } // Галерията
    }
}