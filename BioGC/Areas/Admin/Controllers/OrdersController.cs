using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Orders
        public async Task<IActionResult> Index(string searchTerm, string status, DateTime? startDate, DateTime? endDate)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.ApplicationUser)
                // --- FIX: This line was added to hide subscription orders from this list ---
                .Where(o => o.OrderStatus != "Subscription")
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            // Apply search filter for customer name or email
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ordersQuery = ordersQuery.Where(o => o.ApplicationUser.FullName.Contains(searchTerm) || o.ApplicationUser.Email.Contains(searchTerm));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
            }

            // Apply date range filter
            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                // Add one day to include the entire end date
                ordersQuery = ordersQuery.Where(o => o.OrderDate < endDate.Value.AddDays(1));
            }

            var orders = await ordersQuery.ToListAsync();

            // Create a list of statuses for the dropdown filter
            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending Payment", Text = "Pending Payment" },
                new SelectListItem { Value = "Processing", Text = "Processing" },
                new SelectListItem { Value = "Shipped", Text = "Shipped" },
                new SelectListItem { Value = "Delivered", Text = "Delivered" },
                new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
            };

            // Create and populate the ViewModel
            var viewModel = new OrderIndexViewModel
            {
                Orders = orders,
                Statuses = new SelectList(statusList, "Value", "Text", status),
                SearchTerm = searchTerm,
                Status = status,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: /Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.ShippingZone)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // GET: /Admin/Orders/Invoice/5
        public async Task<IActionResult> Invoice(int? id)
        {
            if (id == null) return NotFound();
            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.ShippingZone)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: /Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            if (string.IsNullOrEmpty(newStatus))
            {
                return BadRequest(new { success = false, message = "New status cannot be empty." });
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { success = false, message = "Order not found." });
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Order status updated successfully." });
        }
    }
}
