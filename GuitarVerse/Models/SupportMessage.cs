using System;
using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class SupportMessage
    {
        [Key]
        public int MessageID { get; set; }

        public int? UserID { get; set; } // Може да е null

        [Required]
        public string SenderName { get; set; }

        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string MessageText { get; set; }

        public string? ReplyText { get; set; } // Твоят отговор (за бъдеще)

        public string Status { get; set; } = "New"; // New, Read

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}