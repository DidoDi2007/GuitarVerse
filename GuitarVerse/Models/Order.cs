using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        public int? UserID { get; set; } // Може да е null (за гости)
        public string? GuestID { get; set; } // За гости

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped

        // --- ДАННИ ЗА ДОСТАВКА ---
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string PostalCode { get; set; }
        [Required]
        public string Country { get; set; }
        public string? TrackingNumber { get; set; }

        // Връзка към детайлите (продуктите в поръчката)
        public List<OrderDetail> OrderDetails { get; set; }
    }
}