using System.Collections.Generic;
using System.Linq;

namespace GuitarVerse.Models
{
    public class CartViewModel
    {
        public List<CartItem> CartItems { get; set; }

        // Стара сума
        public decimal SubTotal => CartItems.Sum(x => x.Quantity * x.Product.Price);

        // Отстъпка
        public int DiscountPercent { get; set; } = 0;
        public string AppliedCode { get; set; }

        // Нова сума (сметната)
        public decimal GrandTotal => SubTotal - (SubTotal * DiscountPercent / 100);
    }
}