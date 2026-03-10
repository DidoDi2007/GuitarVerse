using System;

namespace GuitarVerse.Models
{
    public class PasswordReset
    {
        public int Id { get; set; }

        // foreign key към User
        public int UserId { get; set; }
        public User User { get; set; }

        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool Used { get; set; } = false;
    }
}
