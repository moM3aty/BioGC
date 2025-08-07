using BioGC.Data;
using BioGC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class NotificationsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .OrderByDescending(n => n.Timestamp)
                .Take(5)
                .Select(n => new {
                    n.Id,
                    n.MessageEn,
                    n.MessageAr,
                    n.Url,
                    Timestamp = n.Timestamp.ToLocalTime().ToString("g", CultureInfo.InvariantCulture)
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == user.Id && !n.IsRead);
            return Ok(new { notifications, unreadCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notificationsToUpdate = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead).ToListAsync();

            if (notificationsToUpdate.Any())
            {
                foreach (var notification in notificationsToUpdate) { notification.IsRead = true; }
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }

        [HttpPost("Admin/Notifications/MarkAsRead/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead([FromRoute] int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok(new { success = true });
        }
    }
}
