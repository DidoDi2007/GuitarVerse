namespace GuitarVerse.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } = "customer";
        public bool IsEmailVerified { get; set; } = false;
        public string? EmailVerificationToken { get; set; }



    }
}
