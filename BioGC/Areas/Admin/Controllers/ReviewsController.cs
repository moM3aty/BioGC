using BioGC.Data;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BioGC.Areas.Admin.Controllers
{
    public class ReviewsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reviews
        public async Task<IActionResult> Index(string searchTerm, string status)
        {
            var reviewsQuery = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.ApplicationUser)
                .AsQueryable();

            // Filter by search term (product name or user name)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                reviewsQuery = reviewsQuery.Where(r =>
                    r.Product.NameEn.Contains(searchTerm) ||
                    r.ApplicationUser.FullName.Contains(searchTerm));
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                reviewsQuery = reviewsQuery.Where(r => r.Status == status);
            }

            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pending", Text = "Pending" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Rejected", Text = "Rejected" }
            };

            var viewModel = new ReviewIndexViewModel
            {
                Reviews = await reviewsQuery.OrderByDescending(r => r.DatePosted).ToListAsync(),
                Statuses = new SelectList(statuses, "Value", "Text", status),
                SearchTerm = searchTerm,
                Status = status
            };

            return View(viewModel);
        }

        // POST: Admin/Reviews/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            if (status == "Approved" || status == "Rejected" || status == "Pending")
            {
                review.Status = status;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, newStatus = review.Status });
            }
            return BadRequest(new { success = false, message = "Invalid status provided." });
        }

        // GET: Admin/Reviews/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var review = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.ApplicationUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (review == null) return NotFound();
            return View(review);
        }


        // POST: Admin/Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
