using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

            var revenuePerMonth = await _context.Orders.Where(o => o.OrderDate >= sixMonthsAgo && o.OrderStatus != "Cancelled").GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month }).Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(o => o.TotalAmount) }).OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();
            var revenueLabels = new List<string>();
            var revenueData = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var dataPoint = revenuePerMonth.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
                revenueLabels.Add(date.ToString("MMM yyyy"));
                revenueData.Add(dataPoint?.Total ?? 0);
            }

            // FIX: Exclude the "Digital Services" category from the products chart
            var productsByCategory = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Category != null && p.Category.NameEn != "Digital Services")
                .GroupBy(p => p.Category.NameEn)
                .Select(g => new { CategoryName = g.Key ?? "Uncategorized", Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var orderStatus = await _context.Orders.GroupBy(o => o.OrderStatus).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            var usersByRole = new Dictionary<string, int>();
            var roles = await _roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name);
                usersByRole.Add(role.Name, usersInRole.Count);
            }
            var topProducts = await _context.OrderItems.GroupBy(oi => oi.Product.NameEn).Select(g => new { ProductName = g.Key, Quantity = g.Sum(oi => oi.Quantity) }).OrderByDescending(x => x.Quantity).Take(5).ToListAsync();
            var reviewsStatus = await _context.Reviews.GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.Where(o => o.OrderStatus != "Cancelled").SumAsync(o => o.TotalAmount),
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                RecentOrders = await _context.Orders.Include(o => o.ApplicationUser).OrderByDescending(o => o.OrderDate).Take(5).ToListAsync(),
                RecentUsers = await _userManager.Users.OrderByDescending(u => u.Id).Take(5).ToListAsync(),
                RevenueChartLabels = revenueLabels,
                RevenueChartData = revenueData,
                ProductsByCategoryLabels = productsByCategory.Select(x => x.CategoryName).ToList(),
                ProductsByCategoryData = productsByCategory.Select(x => x.Count).ToList(),
                OrderStatusLabels = orderStatus.Select(x => x.Status).ToList(),
                OrderStatusData = orderStatus.Select(x => x.Count).ToList(),
                UsersByRoleLabels = usersByRole.Keys.ToList(),
                UsersByRoleData = usersByRole.Values.ToList(),
                TopProductsLabels = topProducts.Select(x => x.ProductName).ToList(),
                TopProductsData = topProducts.Select(x => x.Quantity).ToList(),
                ReviewsStatusLabels = reviewsStatus.Select(x => x.Status).ToList(),
                ReviewsStatusData = reviewsStatus.Select(x => x.Count).ToList()
            };
            return View(viewModel);
        }
    }
}
