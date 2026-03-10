using System.ComponentModel.DataAnnotations.Schema;

namespace GuitarVerse.Models
{
    public class OrderDetail
    {
        public int OrderDetailID { get; set; }

        public int OrderID { get; set; }
        public Order Order { get; set; }

        public int ProductID { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; } // Цената В МОМЕНТА на поръчката (важно!)
    }
}