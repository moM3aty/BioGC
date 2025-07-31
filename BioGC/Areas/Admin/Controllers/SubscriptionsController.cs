using BioGC.Data;
using BioGC.Hubs;
using BioGC.Models;
using BioGC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SubscriptionsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly NotificationService _notificationService;
        private readonly ILogger<SubscriptionsController> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public SubscriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, NotificationService notificationService, ILogger<SubscriptionsController> logger, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _notificationService = notificationService;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var subscriptions = await _context.RelaxationSubscriptions.Include(s => s.ApplicationUser).Include(s => s.Order).OrderByDescending(s => s.SubscriptionDate).ToListAsync();
            return View(subscriptions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var subscription = await _context.RelaxationSubscriptions.FindAsync(id);
            if (subscription == null) { TempData["ToastMessage"] = "Error:SubscriptionNotFound"; return RedirectToAction("Index"); }
            var user = await _userManager.FindByIdAsync(subscription.ApplicationUserId);
            if (user == null) { TempData["ToastMessage"] = "Error:UserNotFound"; return RedirectToAction("Index"); }
            if (!await _userManager.IsInRoleAsync(user, "PremiumUser"))
            {
                var result = await _userManager.AddToRoleAsync(user, "PremiumUser");
                if (!result.Succeeded) { TempData["ToastMessage"] = "Error:RoleUpgradeFailed"; return RedirectToAction("Index"); }
                await _signInManager.RefreshSignInAsync(user);
            }
            subscription.Status = "Approved";
            _context.Update(subscription);
            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToUserAsync(user.Id, "Your Relaxation Space subscription is now active!", "تم تفعيل اشتراكك في مساحة الاسترخاء بنجاح!", "/Relaxation/RedirectToContent");
            TempData["ToastMessage"] = "Success:UserUpgraded";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var subscription = await _context.RelaxationSubscriptions.FindAsync(id);
            if (subscription == null) { TempData["ToastMessage"] = "Error:SubscriptionNotFound"; return RedirectToAction("Index"); }

            var user = await _userManager.FindByIdAsync(subscription.ApplicationUserId);
            if (user == null) { TempData["ToastMessage"] = "Error:UserNotFound"; return RedirectToAction("Index"); }

            if (await _userManager.IsInRoleAsync(user, "PremiumUser"))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, "PremiumUser");
                if (!result.Succeeded) { TempData["ToastMessage"] = "Error:RoleDowngradeFailed"; return RedirectToAction("Index"); }

                await _signInManager.RefreshSignInAsync(user);
                await _hubContext.Clients.User(user.Id).SendAsync("SubscriptionStatusChanged");
            }

            subscription.Status = "Cancelled";
            _context.Update(subscription);
            await _context.SaveChangesAsync();

            await _notificationService.SendNotificationToUserAsync(user.Id, "Your Relaxation Space subscription has been cancelled.", "تم إلغاء اشتراكك في مساحة الاسترخاء.", "/Relaxation/RedirectToContent");

            TempData["ToastMessage"] = "Success:SubscriptionCancelled";
            return RedirectToAction("Index");
        }
    }
}