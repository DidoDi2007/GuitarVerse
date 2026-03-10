using System;
using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class User
    {
        public int UserID { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string Role { get; set; } = "customer"; // admin или customer

        public bool IsEmailVerified { get; set; } = false;

        public string? EmailVerificationToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}