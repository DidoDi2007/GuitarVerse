namespace GuitarVerse.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int PendingOrders { get; set; } // Поръчки за изпращане
        public decimal TotalSales { get; set; } // Общ оборот

        // Списък с последните поръчки за бърз преглед
        public List<Order> RecentOrders { get; set; }
    }
}