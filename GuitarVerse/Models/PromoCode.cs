using System.ComponentModel.DataAnnotations;

namespace GuitarVerse.Models
{
    public class PromoCode
    {
        [Key]
        public int PromoID { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } // Ще го направим с главни букви автоматично

        [Required]
        [Range(1, 100)]
        public int DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;
    }
}