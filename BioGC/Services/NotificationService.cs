using BioGC.Data;
using BioGC.Hubs;
using BioGC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        public async Task SendNotificationToAdminsAsync(string messageEn, string messageAr, string url)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                var notification = new Notification
                {
                    MessageEn = messageEn,
                    MessageAr = messageAr,
                    Url = url,
                    UserId = admin.Id
                };
                _context.Notifications.Add(notification);

                await _hubContext.Clients.User(admin.Id).SendAsync("ReceiveNotification", messageEn, messageAr, url);
            }
            await _context.SaveChangesAsync();
        }


        public async Task SendNotificationToUserAsync(string userId, string messageEn, string messageAr, string url)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var notification = new Notification
                {
                    MessageEn = messageEn,
                    MessageAr = messageAr,
                    Url = url,
                    UserId = user.Id
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", messageEn, messageAr, url);
            }
        }
    }
}
