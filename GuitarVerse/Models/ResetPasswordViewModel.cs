namespace GuitarVerse.Models
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }           // добавено
        public string Token { get; set; }           // токенът
        public string NewPassword { get; set; }     // новата парола
        public string ConfirmPassword { get; set; } // потвърждение на паролата
    }
}
