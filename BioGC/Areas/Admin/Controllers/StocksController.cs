using BioGC.Data;
using BioGC.Models;
using BioGC.Services; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StocksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService; 

        public StocksController(ApplicationDbContext context, NotificationService notificationService) 
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string searchTerm, string stockStatus)
        {
            var productsQuery = _context.Products
                .Include(p => p.Stock)
                .OrderBy(p => p.NameEn)
                .AsQueryable();

            var productsWithoutStock = await productsQuery.Where(p => p.Stock == null).ToListAsync();
            if (productsWithoutStock.Any())
            {
                foreach (var product in productsWithoutStock)
                {
                    _context.Stocks.Add(new Stock { ProductId = product.Id, Quantity = 0 });
                }
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.NameEn.Contains(searchTerm) || p.NameAr.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(stockStatus))
            {
                switch (stockStatus)
                {
                    case "in_stock":
                        productsQuery = productsQuery.Where(p => p.Stock.Quantity > 10);
                        break;
                    case "low_stock":
                        productsQuery = productsQuery.Where(p => p.Stock.Quantity > 0 && p.Stock.Quantity <= 10);
                        break;
                    case "out_of_stock":
                        productsQuery = productsQuery.Where(p => p.Stock.Quantity == 0);
                        break;
                }
            }

            ViewData["CurrentSearch"] = searchTerm;
            ViewData["CurrentStatus"] = stockStatus;

            var productsWithStock = await productsQuery.ToListAsync();
            return View(productsWithStock);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int productId, int quantity)
        {
            if (quantity < 0)
            {
                return Json(new { success = false, message = "Quantity cannot be negative." });
            }

            var stockItem = await _context.Stocks.Include(s => s.Product).FirstOrDefaultAsync(s => s.ProductId == productId);
            if (stockItem == null)
            {
                return Json(new { success = false, message = "Stock record not found." });
            }

            stockItem.Quantity = quantity;
            stockItem.LastUpdated = System.DateTime.UtcNow;

            _context.Update(stockItem);
            await _context.SaveChangesAsync();

            if (quantity <= 5)
            {
                await _notificationService.SendNotificationToAdminsAsync(
                    $"Low stock warning for: {stockItem.Product.NameEn}. Only {quantity} items left.",
                    $"تنبيه بانخفاض المخزون لـ: {stockItem.Product.NameAr}. يتبقى {quantity} قطع فقط.",
                    $"/Admin/Stocks?searchTerm={stockItem.Product.NameEn}"
                );
            }

            return Json(new { success = true, message = "Stock updated successfully!" });
        }
    }
}
