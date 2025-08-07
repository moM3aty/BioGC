using BioGC.Data;
using BioGC.Models;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _context.Products
                .Where(p => p.IsListed)
                .Include(p => p.Reviews.Where(r => r.Status == "Approved"))
                .OrderByDescending(p => p.Id)
                .Include(p=>p.Stock)
                .Take(8)
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = featuredProducts
            };

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> _GetFilteredProducts(string sortBy = "all")
        {
            var productsQuery = _context.Products
                                        .Where(p => p.IsListed)
                                        .Include(p => p.Reviews.Where(r => r.Status == "Approved"))
                                        .Include(p => p.OrderItems)
                                        .Include(p => p.Stock)
                                        .AsQueryable();

            switch (sortBy)
            {
                case "best-seller":
                    productsQuery = productsQuery.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity));
                    break;
                case "highest-rated":
                    productsQuery = productsQuery.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0);
                    break;
                case "highest-price":
                    productsQuery = productsQuery.OrderByDescending(p => p.PriceAfterDiscount);
                    break;
                case "lowest-price":
                    productsQuery = productsQuery.OrderBy(p => p.PriceAfterDiscount);
                    break;
                case "offers":
                    productsQuery = productsQuery.Where(p => p.PriceBeforeDiscount > p.PriceAfterDiscount).OrderByDescending(p => (p.PriceBeforeDiscount - p.PriceAfterDiscount) / p.PriceBeforeDiscount);
                    break;
                default: 
                    productsQuery = productsQuery.OrderByDescending(p => p.Id);
                    break;
            }

            var filteredProducts = await productsQuery.Take(8).ToListAsync();

            return PartialView("_FeaturedProductsGrid", filteredProducts);
        }
        public IActionResult cart()
        {
            return View();
        }
        public IActionResult cosmeticsmanufacturing()
        {
            return View();
        }
        public IActionResult courses()
        {
            return View();
        }
        public IActionResult engineeringConsulting()
        {
            return View();
        }
        public IActionResult IT_AI()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
