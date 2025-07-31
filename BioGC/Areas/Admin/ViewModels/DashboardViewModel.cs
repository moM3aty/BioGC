using BioGC.Models;
using System.Collections.Generic;

namespace BioGC.Areas.Admin.ViewModels
{
    /// <summary>
    /// Represents the data required for the Admin Dashboard view.
    /// </summary>
    public class DashboardViewModel
    {
        // --- Top Statistic Cards ---
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }

        // --- Recent Activity Tables ---
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();

        // --- Chart Data ---

        // Revenue Chart (Bar)
        public List<string> RevenueChartLabels { get; set; } = new List<string>();
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();

        // Products by Category (Pie)
        public List<string> ProductsByCategoryLabels { get; set; } = new List<string>();
        public List<int> ProductsByCategoryData { get; set; } = new List<int>();

        // Orders Status (Doughnut)
        public List<string> OrderStatusLabels { get; set; } = new List<string>();
        public List<int> OrderStatusData { get; set; } = new List<int>();

        // Users by Role (PolarArea)
        public List<string> UsersByRoleLabels { get; set; } = new List<string>();
        public List<int> UsersByRoleData { get; set; } = new List<int>();

        // Top 5 Selling Products (Horizontal Bar) - Data for this chart was missing, added it.
        public List<string> TopProductsLabels { get; set; } = new List<string>();
        public List<int> TopProductsData { get; set; } = new List<int>();

        // Reviews Status (Radar)
        public List<string> ReviewsStatusLabels { get; set; } = new List<string>();
        public List<int> ReviewsStatusData { get; set; } = new List<int>();
    }
}
