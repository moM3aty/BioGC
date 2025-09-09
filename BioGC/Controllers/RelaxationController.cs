using BioGC.Data;
using BioGC.Models;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    public class RelaxationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RelaxationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var approvedPackageIds = new HashSet<int>();
            var pendingPackageIds = new HashSet<int>();
            
            var allPackages = await _context.RelaxationPackages
                .Include(p => p.Category)
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category)
                .ToListAsync();

            var viewModel = new RelaxationIndexViewModel
            {
                AllPackages = allPackages,
                ApprovedPackageIds = approvedPackageIds,
                PendingPackageIds = pendingPackageIds
            };

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> MyContent()
        {
            var userId = _userManager.GetUserId(User);
            var userPackageIds = new List<int>();

            if (User.IsInRole("Admin"))
            {
                userPackageIds = await _context.RelaxationPackages.Select(p => p.Id).ToListAsync();
            }
            else
            {
                userPackageIds = await _context.UserRelaxationPackages
                                     .Where(up => up.ApplicationUserId == userId)
                                     .Select(up => up.RelaxationPackageId)
                                     .ToListAsync();
            }

            if (!userPackageIds.Any())
            {
                return View("AccessDenied");
            }

            var packages = await _context.RelaxationPackages
                .Where(p => userPackageIds.Contains(p.Id))
                .Include(p => p.Category)
                .GroupBy(p => p.Category)
                .ToListAsync();

            return View(packages);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            bool hasAccess = await _context.UserRelaxationPackages
                .AnyAsync(p => p.ApplicationUserId == userId && p.RelaxationPackageId == id);

            if (!User.IsInRole("Admin") && !hasAccess)
            {
                return RedirectToAction("AccessDenied");
            }

            var package = await _context.RelaxationPackages
                .Include(p => p.Videos)
                .Include(p => p.Audios)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

