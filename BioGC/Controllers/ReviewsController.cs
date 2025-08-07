using BioGC.Data;
using BioGC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost("Submit")]
        public async Task<IActionResult> SubmitReview([FromForm] int productId, [FromForm] int rating, [FromForm] string comment)
        {
            if (rating < 1 || rating > 5)
            {
                return BadRequest(new { success = false, message = "Invalid rating." });
            }

            var userId = _userManager.GetUserId(User);
            var hasAlreadyReviewed = await _context.Reviews.AnyAsync(r => r.ProductId == productId && r.ApplicationUserId == userId);

            if (hasAlreadyReviewed)
            {
                return BadRequest(new { success = false, message = "You have already reviewed this product." });
            }

            var review = new Review
            {
                ProductId = productId,
                ApplicationUserId = userId,
                Rating = rating,
                Comment = comment,
                DatePosted = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Thank you! Your review has been submitted for approval." });
        }
    }
}