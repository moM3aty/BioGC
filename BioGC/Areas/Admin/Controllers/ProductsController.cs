using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;

namespace BioGC.Areas.Admin.Controllers
{
    public class ProductsController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string searchTerm, int? categoryId)
        {
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.NameEn.Contains(searchTerm) || p.NameAr.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var categoryListItems = new List<SelectListItem>();
            var parentCategories = await _context.Categories
                         .Where(c => c.ParentCategoryId == null && c.NameEn != "Digital Services")
                         .Include(c => c.SubCategories)
                         .OrderBy(c => c.NameEn)
                         .ToListAsync();

            foreach (var parent in parentCategories)
            {
                var group = new SelectListGroup { Name = $"{parent.NameEn} / {parent.NameAr}" };
                if (parent.SubCategories.Any())
                {
                    foreach (var sub in parent.SubCategories.OrderBy(s => s.NameEn))
                    {
                        categoryListItems.Add(new SelectListItem
                        {
                            Value = sub.Id.ToString(),
                            Text = $"-- {sub.NameEn} / {sub.NameAr}",
                            Group = group
                        });
                    }
                }
            }

            var viewModel = new ProductIndexViewModel
            {
                Products = await productsQuery.OrderByDescending(p => p.Id).Where(p => p.IsListed).ToListAsync(),
                Categories = new SelectList(categoryListItems, "Value", "Text", categoryId, "Group.Name"),
                SearchTerm = searchTerm,
                CategoryId = categoryId
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new ProductViewModel
            {
                CategoryList = await GetSubCategorySelectList(),
                Quantity = 0 
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel viewModel)
        {
            ModelState.Remove("CategoryList");
            if (ModelState.IsValid)
            {
                string uniqueFileName = await UploadImage(viewModel.ImageFile);
                var product = new Product
                {
                    NameEn = viewModel.NameEn,
                    NameAr = viewModel.NameAr,
                    DescriptionEn = viewModel.DescriptionEn,
                    DescriptionAr = viewModel.DescriptionAr,
                    PriceBeforeDiscount = viewModel.PriceBeforeDiscount,
                    PriceAfterDiscount = viewModel.PriceAfterDiscount,
                    CategoryId = viewModel.CategoryId,
                    ImageUrl = uniqueFileName,
                    Stock = new Stock { Quantity = viewModel.Quantity }
                };
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            viewModel.CategoryList = await GetSubCategorySelectList();
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            // Include the related Stock data when fetching the product
            var product = await _context.Products.Include(p => p.Stock).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();

            var viewModel = new ProductViewModel
            {
                Id = product.Id,
                NameEn = product.NameEn,
                NameAr = product.NameAr,
                DescriptionEn = product.DescriptionEn,
                DescriptionAr = product.DescriptionAr,
                PriceBeforeDiscount = product.PriceBeforeDiscount,
                PriceAfterDiscount = product.PriceAfterDiscount,
                CategoryId = product.CategoryId,
                ExistingImageUrl = product.ImageUrl,
                // Get the quantity from the related stock record
                Quantity = product.Stock?.Quantity ?? 0,
                CategoryList = await GetSubCategorySelectList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            ModelState.Remove("CategoryList");
            if (ModelState.IsValid)
            {
                var productToUpdate = await _context.Products.Include(p => p.Stock).FirstOrDefaultAsync(p => p.Id == id);
                if (productToUpdate == null) return NotFound();

                if (viewModel.ImageFile != null)
                {
                    if (!string.IsNullOrEmpty(productToUpdate.ImageUrl))
                    {
                        DeleteImage(productToUpdate.ImageUrl);
                    }
                    productToUpdate.ImageUrl = await UploadImage(viewModel.ImageFile);
                }

                productToUpdate.NameEn = viewModel.NameEn;
                productToUpdate.NameAr = viewModel.NameAr;
                productToUpdate.DescriptionEn = viewModel.DescriptionEn;
                productToUpdate.DescriptionAr = viewModel.DescriptionAr;
                productToUpdate.PriceBeforeDiscount = viewModel.PriceBeforeDiscount;
                productToUpdate.PriceAfterDiscount = viewModel.PriceAfterDiscount;
                productToUpdate.CategoryId = viewModel.CategoryId;

                if (productToUpdate.Stock != null)
                {
                    productToUpdate.Stock.Quantity = viewModel.Quantity;
                    productToUpdate.Stock.LastUpdated = System.DateTime.UtcNow;
                }
                else 
                {
                    productToUpdate.Stock = new Stock { Quantity = viewModel.Quantity };
                }

                try
                {
                    _context.Update(productToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(productToUpdate.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            viewModel.CategoryList = await GetSubCategorySelectList();
            return View(viewModel);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    DeleteImage(product.ImageUrl);
                }
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategorySelectListJson()
        {
            var selectList = await GetSubCategorySelectList();
            return Json(selectList);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        private async Task<string> UploadImage(IFormFile imageFile)
        {
            if (imageFile == null) return null;

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return uniqueFileName;
        }

        private void DeleteImage(string fileName)
        {
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/products", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private async Task<IEnumerable<SelectListItem>> GetSubCategorySelectList()
        {
            var lang = Request.Cookies["language"] ?? "en";

            return await _context.Categories
                .Where(c => c.ParentCategoryId != null)
                .OrderBy(c => c.NameEn)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = lang == "ar" ? c.NameAr : c.NameEn
                }).ToListAsync();
        }
    }
}
