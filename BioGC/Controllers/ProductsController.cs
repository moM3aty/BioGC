using BioGC.Data;
using BioGC.Models;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BioGC.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> List(int? categoryId, string searchTerm, string sortBy = "all")
        {
            // Start with a query for all products, including their reviews for rating calculation
            var productsQuery = _context.Products.Where(p => p.IsListed)
                                        .Include(p => p.Reviews.Where(r => r.Status == "Approved"))
                                        .Include(p => p.OrderItems) 
                                        .Include(p => p.Stock)
                                        .AsQueryable();

            // Filter by category if one is selected
            if (categoryId.HasValue && categoryId > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Filter by search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.NameEn.Contains(searchTerm) || p.NameAr.Contains(searchTerm) || p.DescriptionEn.Contains(searchTerm) || p.DescriptionAr.Contains(searchTerm));
            }

            // Apply sorting based on the sortBy parameter
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

            var viewModel = new ProductListViewModel
            {
                Products = await productsQuery.ToListAsync(),
                AllCategories = await _context.Categories.Where(c => c.ParentCategoryId != null).OrderBy(c => c.NameEn).ToListAsync(),
                CurrentCategory = categoryId.HasValue ? await _context.Categories.FindAsync(categoryId.Value) : null,
                SearchTerm = searchTerm,
                SortBy = sortBy
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews.Where(r => r.Status == "Approved"))
                .ThenInclude(r => r.ApplicationUser)
                .Include(p => p.Stock)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var relatedProducts = await _context.Products
                .Where(p => p.IsListed)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Include(p => p.Reviews.Where(r => r.Status == "Approved"))
                .Include(p => p.Stock)
                .Take(4)
                .ToListAsync();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                CanUserReview = false
            };

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var hasReviewed = await _context.Reviews.AnyAsync(r => r.ProductId == id && r.ApplicationUserId == userId);
                viewModel.CanUserReview = !hasReviewed;
            }

            return View(viewModel);
        }
    }
}
