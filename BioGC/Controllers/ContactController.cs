using BioGC.Data;
using BioGC.Models;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                var message = new ContactMessage
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    SourcePage = model.Service,
                    ProductCategory = model.Request,
                    Message = model.Message,
                    SubmittedAt = System.DateTime.UtcNow
                };

                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    message.ApplicationUserId = user.Id;
                }

                _context.ContactMessages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Message sent successfully!" });
            }

            return Json(new { success = false, message = "Invalid data. Please check the form." });
        }
    }
}
