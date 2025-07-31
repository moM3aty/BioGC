using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RelaxationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RelaxationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var content = await _context.RelaxationContents
                .Include(c => c.Videos.OrderBy(v => v.Title))
                .Include(c => c.Audios.OrderBy(a => a.Title))
                .FirstOrDefaultAsync();

            if (content == null)
            {
                content = new RelaxationContent { ProductId = 1 };
                _context.RelaxationContents.Add(content);
                await _context.SaveChangesAsync();
            }
            else if (!content.ProductId.HasValue)
            {
                content.ProductId = 1;
                _context.RelaxationContents.Update(content);
                await _context.SaveChangesAsync();
            }

            Product associatedProduct = null;
            if (content.ProductId.HasValue)
            {
                associatedProduct = await _context.Products.FindAsync(content.ProductId.Value);
            }

            var viewModel = new RelaxationAdminViewModel
            {
                Content = content,
                AssociatedProduct = associatedProduct
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePrice(int productId, decimal price)
        {
            var productToUpdate = await _context.Products.FindAsync(productId);
            if (productToUpdate != null)
            {
                productToUpdate.PriceAfterDiscount = price;
                productToUpdate.PriceBeforeDiscount = price;
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Success:PriceUpdated";
            }
            else
            {
                TempData["ToastMessage"] = "Error:ProductNotFound";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVideo(int contentId, string title, int libraryId, string videoGuid)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(videoGuid) || libraryId <= 0)
            {
                TempData["ToastMessage"] = "Error:VideoFieldsRequired";
                return RedirectToAction("Index");
            }
            var video = new RelaxationVideo { Title = title, LibraryId = libraryId, VideoGuid = videoGuid, RelaxationContentId = contentId };
            _context.RelaxationVideos.Add(video);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Success:VideoAdded";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var video = await _context.RelaxationVideos.FindAsync(id);
            if (video != null)
            {
                _context.RelaxationVideos.Remove(video);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Success:VideoDeleted";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAudio(int contentId, string title, int libraryId, string audioGuid)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(audioGuid) || libraryId <= 0)
            {
                TempData["ToastMessage"] = "Error:AudioFieldsRequired";
                return RedirectToAction("Index");
            }
            var audio = new RelaxationAudio { Title = title, LibraryId = libraryId, AudioGuid = audioGuid, RelaxationContentId = contentId };
            _context.RelaxationAudios.Add(audio);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Success:AudioAdded";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAudio(int id)
        {
            var audio = await _context.RelaxationAudios.FindAsync(id);
            if (audio != null)
            {
                _context.RelaxationAudios.Remove(audio);
                await _context.SaveChangesAsync();
                TempData["ToastMessage"] = "Success:AudioDeleted";
            }
            return RedirectToAction("Index");
        }
    }
}
