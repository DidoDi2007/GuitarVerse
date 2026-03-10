using System;
using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class Review
    {
        public int ReviewID { get; set; }

        public int ProductID { get; set; }
        public Product Product { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } // За да показваме името на човека

        [Range(1, 5)]
        public int Rating { get; set; } // 1 до 5 звезди

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(900, ErrorMessage = "Review cannot exceed 900 characters.")]
        public string Comment { get; set; }
    }
}