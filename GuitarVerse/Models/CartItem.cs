using System;

namespace GuitarVerse.Models
{
    public class CartItem
    {
        public int CartItemID { get; set; }

        // Слагаме ? (nullable), защото може да нямаме регистриран юзър
        public int? UserID { get; set; }

        // Новото поле за гости
        public string? GuestID { get; set; }

        public int ProductID { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}