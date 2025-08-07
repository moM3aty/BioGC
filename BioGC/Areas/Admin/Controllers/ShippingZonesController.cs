using BioGC.Data;
using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    public class ShippingZonesController : AdminBaseController
    {
        private readonly ApplicationDbContext _context;

        public ShippingZonesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ShippingZones
        public async Task<IActionResult> Index(string searchTerm)
        {
            var query = _context.ShippingZones.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(z => z.ZoneNameEn.Contains(searchTerm) || z.ZoneNameAr.Contains(searchTerm));
            }

            var viewModel = new ShippingZoneIndexViewModel
            {
                ShippingZones = await query.OrderBy(z => z.ZoneNameEn).ToListAsync(),
                SearchTerm = searchTerm
            };

            return View(viewModel);
        }

        // GET: Admin/ShippingZones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ShippingZones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShippingZoneViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var shippingZone = new ShippingZone
                {
                    ZoneNameEn = viewModel.ZoneNameEn,
                    ZoneNameAr = viewModel.ZoneNameAr,
                    ShippingCost = viewModel.ShippingCost
                };
                _context.Add(shippingZone);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Admin/ShippingZones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var shippingZone = await _context.ShippingZones.FindAsync(id);
            if (shippingZone == null) return NotFound();

            var viewModel = new ShippingZoneViewModel
            {
                Id = shippingZone.Id,
                ZoneNameEn = shippingZone.ZoneNameEn,
                ZoneNameAr = shippingZone.ZoneNameAr,
                ShippingCost = shippingZone.ShippingCost
            };
            return View(viewModel);
        }

        // POST: Admin/ShippingZones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ShippingZoneViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var shippingZone = await _context.ShippingZones.FindAsync(id);
                    shippingZone.ZoneNameEn = viewModel.ZoneNameEn;
                    shippingZone.ZoneNameAr = viewModel.ZoneNameAr;
                    shippingZone.ShippingCost = viewModel.ShippingCost;
                    _context.Update(shippingZone);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShippingZoneExists(viewModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        // GET: Admin/ShippingZones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var shippingZone = await _context.ShippingZones.FirstOrDefaultAsync(m => m.Id == id);
            if (shippingZone == null) return NotFound();
            return View(shippingZone);
        }

        // POST: Admin/ShippingZones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shippingZone = await _context.ShippingZones.FindAsync(id);
            _context.ShippingZones.Remove(shippingZone);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShippingZoneExists(int id)
        {
            return _context.ShippingZones.Any(e => e.Id == id);
        }
    }
}
