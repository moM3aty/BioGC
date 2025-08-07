using BioGC.Data;

using BioGC.Models;

using BioGC.ViewModels;

using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;



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



        public async Task<IActionResult> List(int? parentCategoryId, int? subCategoryId, string searchTerm, string sortBy = "all")

        {

            var productsQuery = _context.Products

              .Where(p => p.IsListed)

              .Include(p => p.Reviews.Where(r => r.Status == "Approved"))

              .Include(p => p.OrderItems)

              .Include(p => p.Stock)

              .Include(p => p.Category)

              .AsQueryable();



            if (subCategoryId.HasValue && subCategoryId > 0)

            {

                productsQuery = productsQuery.Where(p => p.CategoryId == subCategoryId.Value);

            }

            else if (parentCategoryId.HasValue && parentCategoryId > 0)

            {

                productsQuery = productsQuery.Where(p => p.Category.ParentCategoryId == parentCategoryId.Value);

            }



            if (!string.IsNullOrEmpty(searchTerm))

            {

                productsQuery = productsQuery.Where(p => p.NameEn.Contains(searchTerm) || p.NameAr.Contains(searchTerm) || p.DescriptionEn.Contains(searchTerm) || p.DescriptionAr.Contains(searchTerm));

            }



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



            // --- Data for UI (Sidebar and Tabs) ---

            var parentCategories = await _context.Categories

           .Where(c => c.ParentCategoryId == null && c.NameEn != "Digital Services")

           .OrderBy(c => c.NameEn)

           .ToListAsync();



            var subCategoryTabs = new List<Category>();

            int? currentParentId = parentCategoryId;



            // If a sub-category is selected, we must find its parent to correctly highlight the sidebar and show the tabs.

            if (subCategoryId.HasValue && subCategoryId > 0 && !parentCategoryId.HasValue)

            {

                var selectedSubCategory = await _context.Categories.FindAsync(subCategoryId.Value);

                if (selectedSubCategory?.ParentCategoryId != null)

                {

                    currentParentId = selectedSubCategory.ParentCategoryId;

                }

            }



            // If a parent category is active (either directly or inferred), fetch its children for the tabs.

            if (currentParentId.HasValue)

            {

                subCategoryTabs = await _context.Categories

                  .Where(c => c.ParentCategoryId == currentParentId.Value)

                  .OrderBy(c => c.NameEn)

                  .ToListAsync();

            }



            var viewModel = new ProductListViewModel

            {

                Products = await productsQuery.ToListAsync(),

                ParentCategories = parentCategories,

                SubCategoryTabs = subCategoryTabs,

                SelectedParentCategoryId = currentParentId,

                SelectedSubCategoryId = subCategoryId,

                SearchTerm = searchTerm,

                SortBy = sortBy

            };



            return View(viewModel);

        }



        public async Task<IActionResult> Details(int id)

        {

            var product = await _context.Products.Include(p => p.Category).Include(p => p.Reviews.Where(r => r.Status == "Approved")).ThenInclude(r => r.ApplicationUser).Include(p => p.Stock).FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var relatedProducts = await _context.Products.Where(p => p.IsListed).Where(p => p.CategoryId == product.CategoryId && p.Id != id).Include(p => p.Reviews.Where(r => r.Status == "Approved")).Include(p => p.Stock).Take(4).ToListAsync();

            var viewModel = new ProductDetailViewModel { Product = product, RelatedProducts = relatedProducts, CanUserReview = false };

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