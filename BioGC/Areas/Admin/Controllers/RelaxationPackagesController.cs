using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RelaxationPackagesController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RelaxationPackagesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.RelaxationPackages
                .Include(p => p.Category)
                .OrderBy(p => p.TitleEn)
                .ToListAsync();
            return View(packages);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new RelaxationPackageViewModel
            {
                CategoryList = await GetRelaxationCategoriesSelectList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RelaxationPackageViewModel vm)
        {
            ModelState.Remove("CategoryList");
            ModelState.Remove("ExistingImageUrl");
            if (ModelState.IsValid)
            {
                string uniqueFileName = await UploadImage(vm.ImageFile);
                var package = new RelaxationPackage
                {
                    TitleEn = vm.TitleEn,
                    TitleAr = vm.TitleAr,
                    DescriptionEn = vm.DescriptionEn,
                    DescriptionAr = vm.DescriptionAr,
                    Price = vm.Price,
                    CategoryId = vm.CategoryId,
                    ImageUrl = uniqueFileName
                };
                _context.Add(package);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            vm.CategoryList = await GetRelaxationCategoriesSelectList(vm.CategoryId);
            return View(vm);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var package = await _context.RelaxationPackages.FindAsync(id);
            if (package == null) return NotFound();

            var vm = new RelaxationPackageViewModel
            {
                Id = package.Id,
                TitleEn = package.TitleEn,
                TitleAr = package.TitleAr,
                DescriptionEn = package.DescriptionEn,
                DescriptionAr = package.DescriptionAr,
                Price = package.Price,
                CategoryId = package.CategoryId,
                ExistingImageUrl = package.ImageUrl,
                CategoryList = await GetRelaxationCategoriesSelectList(package.CategoryId)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RelaxationPackageViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            ModelState.Remove("CategoryList");
            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                var packageToUpdate = await _context.RelaxationPackages.FindAsync(id);
                if (packageToUpdate == null) return NotFound();

                if (vm.ImageFile != null)
                {
                    DeleteImage(packageToUpdate.ImageUrl);
                    packageToUpdate.ImageUrl = await UploadImage(vm.ImageFile);
                }

                packageToUpdate.TitleEn = vm.TitleEn;
                packageToUpdate.TitleAr = vm.TitleAr;
                packageToUpdate.DescriptionEn = vm.DescriptionEn;
                packageToUpdate.DescriptionAr = vm.DescriptionAr;
                packageToUpdate.Price = vm.Price;
                packageToUpdate.CategoryId = vm.CategoryId;

                _context.Update(packageToUpdate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            vm.CategoryList = await GetRelaxationCategoriesSelectList(vm.CategoryId);
            return View(vm);
        }

        public async Task<IActionResult> ManageMedia(int id)
        {
            var package = await _context.RelaxationPackages
                .Include(p => p.Videos)
                .Include(p => p.Audios)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null) return NotFound();

            var lang = Request.Cookies["language"] ?? "en";
            var vm = new RelaxationMediaViewModel
            {
                PackageId = package.Id,
                PackageTitle = lang == "ar" ? package.TitleAr : package.TitleEn,
                Videos = package.Videos,
                Audios = package.Audios
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVideo(RelaxationMediaViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.NewItemTitle) && !string.IsNullOrEmpty(vm.NewItemLibraryId) && !string.IsNullOrEmpty(vm.NewItemGuid))
            {
                if (!int.TryParse(vm.NewItemLibraryId, out int parsedLibraryId))
                {
                    TempData["MediaError"] = "Error: Library ID must be a valid number.";
                    return RedirectToAction("ManageMedia", new { id = vm.PackageId });
                }

                var video = new RelaxationVideo
                {
                    Title = vm.NewItemTitle,
                    LibraryId = parsedLibraryId,
                    VideoGuid = vm.NewItemGuid,
                    RelaxationPackageId = vm.PackageId
                };
                _context.RelaxationVideos.Add(video);
                await _context.SaveChangesAsync();
                TempData["MediaSuccess"] = "Video added successfully.";
            }
            else
            {
                TempData["MediaError"] = "Error: All fields are required to add a new video.";
            }
            return RedirectToAction("ManageMedia", new { id = vm.PackageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAudio(RelaxationMediaViewModel vm)
        {
            if (!string.IsNullOrEmpty(vm.NewItemTitle) && !string.IsNullOrEmpty(vm.NewItemLibraryId) && !string.IsNullOrEmpty(vm.NewItemGuid))
            {
                if (!int.TryParse(vm.NewItemLibraryId, out int parsedLibraryId))
                {
                    TempData["MediaError"] = "Error: Library ID must be a valid number.";
                    return RedirectToAction("ManageMedia", new { id = vm.PackageId });
                }

                var audio = new RelaxationAudio
                {
                    Title = vm.NewItemTitle,
                    LibraryId = parsedLibraryId,
                    AudioGuid = vm.NewItemGuid,
                    RelaxationPackageId = vm.PackageId
                };
                _context.RelaxationAudios.Add(audio);
                await _context.SaveChangesAsync();
                TempData["MediaSuccess"] = "Audio added successfully.";
            }
            else
            {
                TempData["MediaError"] = "Error: All fields are required to add new audio.";
            }
            return RedirectToAction("ManageMedia", new { id = vm.PackageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id, int packageId)
        {
            var video = await _context.RelaxationVideos.FindAsync(id);
            if (video != null)
            {
                _context.RelaxationVideos.Remove(video);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageMedia", new { id = packageId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAudio(int id, int packageId)
        {
            var audio = await _context.RelaxationAudios.FindAsync(id);
            if (audio != null)
            {
                _context.RelaxationAudios.Remove(audio);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageMedia", new { id = packageId });
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var package = await _context.RelaxationPackages.Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (package == null) return NotFound();
            return View(package);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.RelaxationPackages.FindAsync(id);
            if (package != null)
            {
                DeleteImage(package.ImageUrl);
                _context.RelaxationPackages.Remove(package);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper Methods
        private async Task<SelectList> GetRelaxationCategoriesSelectList(int? selectedId = null)
        {
            var lang = Request.Cookies["language"] ?? "en";
            var relaxationParentCategory = await _context.Categories.FirstOrDefaultAsync(c => c.NameEn == "Relaxation Programs");
            if (relaxationParentCategory == null)
            {
                return new SelectList(Enumerable.Empty<SelectListItem>());
            }
            var items = await _context.Categories
                .Where(c => c.ParentCategoryId == relaxationParentCategory.Id)
                .OrderBy(c => c.NameEn)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = lang == "ar" ? c.NameAr : c.NameEn
                }).ToListAsync();

            return new SelectList(items, "Value", "Text", selectedId);
        }
        private async Task<string> UploadImage(IFormFile imageFile)
        {
            if (imageFile == null) return "default.jpg";
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/packages");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
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
            if (string.IsNullOrEmpty(fileName) || fileName == "default.jpg") return;
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/packages", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}

