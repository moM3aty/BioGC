using BioGC.Data;
using BioGC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BioGC.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("MyFavorites")]
        public async Task<IActionResult> GetUserFavorites()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var favoriteIds = await _context.Wishlists
                .Where(w => w.ApplicationUserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            return Ok(favoriteIds);
        }

        [HttpPost("ToggleFavorite/{productId}")]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == productId);

            if (wishlistItem == null)
            {
                _context.Wishlists.Add(new Wishlist { ApplicationUserId = userId, ProductId = productId });
                await _context.SaveChangesAsync();
                return Ok(new { status = "added", message = "Added to wishlist." });
            }
            else
            {
                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Ok(new { status = "removed", message = "Removed from wishlist." });
            }
        }

        [HttpDelete("Remove/{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == productId);

            if (wishlistItem != null)
            {
                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Item removed from wishlist." });
            }
            return NotFound();
        }
    }
}
