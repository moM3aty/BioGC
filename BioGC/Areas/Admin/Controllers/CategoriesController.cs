using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;

namespace BioGC.Areas.Admin.Controllers
{
    public class CategoriesController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Standard Views (No Changes Here)
        public async Task<IActionResult> Index(string searchTerm)
        {
            var parentCategoriesQuery = _context.Categories
                .Where(c => c.ParentCategoryId == null && c.NameEn != "Digital Services")
                .Include(c => c.SubCategories)
                .OrderBy(c => c.NameEn)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                parentCategoriesQuery = parentCategoriesQuery.Where(p =>
                    p.NameEn.Contains(searchTerm) ||
                    p.NameAr.Contains(searchTerm) ||
                    p.SubCategories.Any(s => s.NameEn.Contains(searchTerm) || s.NameAr.Contains(searchTerm))
                );
            }

            var viewModel = new CategoryIndexViewModel
            {
                ParentCategories = await parentCategoriesQuery.ToListAsync(),
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CategoryViewModel { ParentCategoryList = await GetParentCategorySelectList() };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel model)
        {
            ModelState.Remove("ParentCategoryList");
            if (ModelState.IsValid)
            {
                var category = new Category { NameEn = model.NameEn, NameAr = model.NameAr, ParentCategoryId = model.ParentCategoryId };
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            model.ParentCategoryList = await GetParentCategorySelectList();
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            var viewModel = new CategoryViewModel { Id = category.Id, NameEn = category.NameEn, NameAr = category.NameAr, ParentCategoryId = category.ParentCategoryId, ParentCategoryList = await GetParentCategorySelectList(category.Id) };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryViewModel model)
        {
            if (id != model.Id) return NotFound();
            ModelState.Remove("ParentCategoryList");

            if (ModelState.IsValid)
            {
                try
                {
                    var categoryToUpdate = await _context.Categories.FindAsync(id);
                    if (categoryToUpdate == null) return NotFound();

                    categoryToUpdate.NameEn = model.NameEn;
                    categoryToUpdate.NameAr = model.NameAr;
                    categoryToUpdate.ParentCategoryId = model.ParentCategoryId;

                    _context.Update(categoryToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(model.Id)) return NotFound(); else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            model.ParentCategoryList = await GetParentCategorySelectList(model.Id);
            return View(model);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.Include(c => c.ParentCategory).FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Id == id);
            if (category != null)
            {
                if (category.SubCategories.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete a parent category that has sub-categories. Please remove or re-assign the sub-categories first.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Parent Category API (No Changes Here)
        [HttpGet]
        public async Task<IActionResult> GetParentCategoriesJson()
        {
            var categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.NameEn)
                .Select(c => new { id = c.Id, name = c.NameEn + " / " + c.NameAr })
                .ToListAsync();
            return Json(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateParentJson([FromBody] CategoryApiViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = new Category { NameEn = model.NameEn, NameAr = model.NameAr, ParentCategoryId = null };
                _context.Add(category);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            return BadRequest(new { success = false, message = "Invalid data provided." });
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditParentJson([FromBody] CategoryApiViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = await _context.Categories.FindAsync(model.Id);
                if (category == null) return NotFound();
                category.NameEn = model.NameEn;
                category.NameAr = model.NameAr;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            return BadRequest(new { success = false, message = "Invalid data provided." });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteParentJson(int id)
        {
            var category = await _context.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            if (category.SubCategories.Any())
            {
                return BadRequest(new { success = false, message = "Cannot delete a category that has sub-categories." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        #endregion

        #region Sub-Category API (New and Modified Actions)

        [HttpGet]
        public async Task<IActionResult> GetCategoryJson(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return Json(new { id = category.Id, nameEn = category.NameEn, nameAr = category.NameAr, parentCategoryId = category.ParentCategoryId });
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategoriesJson(int parentId)
        {
            var categories = await _context.Categories
                .Where(c => c.ParentCategoryId == parentId)
                .OrderBy(c => c.NameEn)
                .Select(c => new { id = c.Id, name = c.NameEn + " / " + c.NameAr })
                .ToListAsync();
            return Json(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubCategoryJson([FromBody] SubCategoryApiViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = new Category
                {
                    NameEn = model.NameEn,
                    NameAr = model.NameAr,
                    ParentCategoryId = model.ParentCategoryId
                };
                _context.Add(category);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, newCategory = new { id = category.Id, nameEn = category.NameEn, nameAr = category.NameAr } });
            }
            return BadRequest(new { success = false, message = "Invalid data provided." });
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubCategoryJson([FromBody] SubCategoryApiViewModel model)
        {
            if (ModelState.IsValid)
            {
                var category = await _context.Categories.FindAsync(model.Id);
                if (category == null) return NotFound();

                category.NameEn = model.NameEn;
                category.NameAr = model.NameAr;
                category.ParentCategoryId = model.ParentCategoryId;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            return BadRequest(new { success = false, message = "Invalid data provided." });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubCategoryJson(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();

            if (category.Products.Any())
            {
                return BadRequest(new { success = false, message = "Cannot delete a sub-category that is linked to products." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        #endregion

        #region Private Helper Methods
        private bool CategoryExists(int id) => _context.Categories.Any(e => e.Id == id);

        private async Task<IEnumerable<SelectListItem>> GetParentCategorySelectList(int? currentCategoryId = null)
        {
            var query = _context.Categories.Where(c => c.ParentCategoryId == null && c.NameEn != "Digital Services");
            if (currentCategoryId.HasValue)
            {
                query = query.Where(c => c.Id != currentCategoryId.Value);
            }
            var lang = Request.Cookies["language"] ?? "en";

            return await query.OrderBy(c => c.NameEn).Select(c => new SelectListItem { Value = c.Id.ToString(), Text = lang == "ar" ? c.NameAr : c.NameEn }).ToListAsync();
        }
        #endregion
    }
}
