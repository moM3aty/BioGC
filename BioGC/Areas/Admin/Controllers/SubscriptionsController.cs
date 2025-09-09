using BioGC.Data;
using BioGC.Models;
using BioGC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SubscriptionsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager; // Add UserManager

        public SubscriptionsController(ApplicationDbContext context, NotificationService notificationService, UserManager<ApplicationUser> userManager) // Inject UserManager
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager; // Assign it
        }

        public async Task<IActionResult> Index()
        {
            var subscriptions = await _context.RelaxationSubscriptions
                .Include(s => s.ApplicationUser)
                .Include(s => s.Order)
                .Include(s => s.RelaxationPackage)
                .OrderByDescending(s => s.SubscriptionDate)
                .ToListAsync();
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

            // 1. Grant access by adding a record to UserRelaxationPackages
            var userHasPackage = await _context.UserRelaxationPackages.AnyAsync(p =>
                p.ApplicationUserId == subscription.ApplicationUserId &&
                p.RelaxationPackageId == subscription.RelaxationPackageId);

            if (!userHasPackage)
            {
                _context.UserRelaxationPackages.Add(new UserRelaxationPackage
                {
                    ApplicationUserId = subscription.ApplicationUserId,
                    RelaxationPackageId = subscription.RelaxationPackageId
                });
            }

            // 2. Upgrade user role to PremiumUser if they aren't already
            if (!await _userManager.IsInRoleAsync(user, "PremiumUser"))
            {
                await _userManager.AddToRoleAsync(user, "PremiumUser");
            }

            // 3. Update subscription status
            subscription.Status = "Approved";
            _context.Update(subscription);
            await _context.SaveChangesAsync();

            // 4. Notify user
            var package = await _context.RelaxationPackages.FindAsync(subscription.RelaxationPackageId);
            await _notificationService.SendNotificationToUserAsync(
                subscription.ApplicationUserId,
                $"Your subscription for '{package.TitleEn}' is now active!",
                $"تم تفعيل اشتراكك في '{package.TitleAr}' بنجاح!",
                "/Relaxation/MyContent");

            TempData["ToastMessage"] = "Success:SubscriptionApproved";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var subscription = await _context.RelaxationSubscriptions.FindAsync(id);
            if (subscription == null) { TempData["ToastMessage"] = "Error:SubscriptionNotFound"; return RedirectToAction("Index"); }

            // 1. Revoke access by removing the record from UserRelaxationPackages
            var userPackageAccess = await _context.UserRelaxationPackages.FirstOrDefaultAsync(p =>
                p.ApplicationUserId == subscription.ApplicationUserId &&
                p.RelaxationPackageId == subscription.RelaxationPackageId);

            if (userPackageAccess != null)
            {
                _context.UserRelaxationPackages.Remove(userPackageAccess);
            }

            // 2. Update subscription status
            subscription.Status = "Cancelled";
            _context.Update(subscription);
            await _context.SaveChangesAsync();

            // 3. Notify user
            var package = await _context.RelaxationPackages.FindAsync(subscription.RelaxationPackageId);
            await _notificationService.SendNotificationToUserAsync(
                 subscription.ApplicationUserId,
                 $"Your subscription for '{package.TitleEn}' has been cancelled.",
                 $"تم إلغاء اشتراكك في '{package.TitleAr}'.",
                 "/Relaxation/Index");

            TempData["ToastMessage"] = "Success:SubscriptionCancelled";
            return RedirectToAction("Index");
        }
    }
}

