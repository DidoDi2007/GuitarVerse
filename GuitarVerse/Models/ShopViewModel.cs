using System.Collections.Generic;

namespace GuitarVerse.Models
{
    public class ShopViewModel
    {
        public IEnumerable<Product> Products { get; set; }

        // Списъци за филтрите
        public IEnumerable<Category> Categories { get; set; }
        public List<string> Brands { get; set; }

        // Текущо избрани стойности
        public int? CurrentCategoryId { get; set; }
        public string CurrentBrand { get; set; }
        public List<string> SelectedBrands { get; set; } = new List<string>(); // Множествен избор (Checkbox)

        public string SearchQuery { get; set; }
        public string SortOrder { get; set; }

        // Нови филтри (засега единичен избор за простота, или range)
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string SelectedOrientation { get; set; }
        public int? SelectedStrings { get; set; }
        public string SelectedBridge { get; set; }
        public string SelectedPickup { get; set; }
    }
}