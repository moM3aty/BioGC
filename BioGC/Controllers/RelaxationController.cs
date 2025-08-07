using BioGC.Data;
using BioGC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    public class RelaxationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RelaxationController> _logger;

        public RelaxationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<RelaxationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> RedirectToContent()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Offer");
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("Admin") || userRoles.Contains("PremiumUser"))
                {
                    return RedirectToAction("Index");
                }

                var hasPendingSubscription = await _context.RelaxationSubscriptions
                    .AnyAsync(s => s.ApplicationUserId == user.Id && s.Status == "Pending Approval");

                if (hasPendingSubscription)
                {
                    return RedirectToAction("SubscriptionPending");
                }
            }

            return RedirectToAction("Offer");
        }

        [Authorize(Roles = "Admin,PremiumUser")]
        public async Task<IActionResult> Index()
        {
            var content = await _context.RelaxationContents
                .Include(c => c.Videos.OrderBy(v => v.Title))
                .Include(c => c.Audios.OrderBy(a => a.Title))
                .FirstOrDefaultAsync();

            if (content == null || (!content.Videos.Any() && !content.Audios.Any()))
            {
                return View("ContentNotAvailable");
            }

            return View(content);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Offer()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("PremiumUser"))
                    {
                        return RedirectToAction("Index");
                    }

                    var hasPendingSubscription = await _context.RelaxationSubscriptions
                        .AnyAsync(s => s.ApplicationUserId == user.Id && s.Status == "Pending Approval");

                    if (hasPendingSubscription)
                    {
                        return RedirectToAction("SubscriptionPending");
                    }
                }
            }


            var relaxationServiceProduct = await _context.Products.FindAsync(1);

            if (relaxationServiceProduct == null)
            {
                _logger.LogError("CRITICAL: The Relaxation Service Product with ID=1 was not found in the database. The offer page cannot be displayed.");
                return View("ContentNotAvailable");
            }

            return View(relaxationServiceProduct);
        }

        [Authorize]
        public IActionResult SubscriptionPending()
        {
            return View();
        }
    }
}
