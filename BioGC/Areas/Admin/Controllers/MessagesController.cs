using BioGC.Data;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    public class MessagesController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Messages
        public async Task<IActionResult> Index(string searchTerm, string sourcePage, DateTime? startDate, DateTime? endDate)
        {
            var messagesQuery = _context.ContactMessages.AsQueryable();

            // Apply search term filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                messagesQuery = messagesQuery.Where(m => m.FullName.Contains(searchTerm) || m.Email.Contains(searchTerm));
            }

            // Apply source page filter
            if (!string.IsNullOrEmpty(sourcePage))
            {
                messagesQuery = messagesQuery.Where(m => m.SourcePage == sourcePage);
            }

            // Apply date range filter
            if (startDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.SubmittedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                messagesQuery = messagesQuery.Where(m => m.SubmittedAt < endDate.Value.AddDays(1));
            }

            // Get distinct source pages for the filter dropdown
            var distinctSourcePages = await _context.ContactMessages
                .Select(m => m.SourcePage)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            var viewModel = new MessageIndexViewModel
            {
                Messages = await messagesQuery.OrderByDescending(m => m.SubmittedAt).ToListAsync(),
                SourcePages = new SelectList(distinctSourcePages, sourcePage),
                SearchTerm = searchTerm,
                SourcePage = sourcePage,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: Admin/Messages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var contactMessage = await _context.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (contactMessage == null) return NotFound();
            return View(contactMessage);
        }

        // GET: Admin/Messages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var contactMessage = await _context.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (contactMessage == null) return NotFound();
            return View(contactMessage);
        }

        // POST: Admin/Messages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contactMessage = await _context.ContactMessages.FindAsync(id);
            if (contactMessage != null)
            {
                _context.ContactMessages.Remove(contactMessage);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
