using System.Collections.Generic;
using System.Linq;

namespace GuitarVerse.Models
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; }

        // Изчисляваме общата сума автоматично
        public decimal GrandTotal => CartItems.Sum(x => x.Quantity * x.Product.Price);
    }
}