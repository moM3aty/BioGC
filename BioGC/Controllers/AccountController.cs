using BioGC.Data;
using BioGC.Models;
using BioGC.Services;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BioGC.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly NotificationService _notificationService;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, NotificationService notificationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            var lang = Request.Cookies["language"] ?? "en";
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    string errorMessage = (lang == "ar") ? "بيانات تسجيل الدخول غير صحيحة." : "Invalid login attempt.";
                    ModelState.AddModelError(string.Empty, errorMessage);
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            var lang = Request.Cookies["language"] ?? "en";
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    await _notificationService.SendNotificationToAdminsAsync(
                        $"New user '{user.UserName}' has registered.",
                        $"تم تسجيل مستخدم جديد '{user.UserName}'.",
                        $"/Admin/Users"
                    );
                    return RedirectToLocal(returnUrl);
                }
                AddIdentityErrors(result, lang);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new ProfileViewModel();
            await PopulateProfileViewModel(model, user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await RepopulateViewModelForError(model, user);
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                await RepopulateViewModelForError(model, user); 
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.NewPassword) && !string.IsNullOrEmpty(model.OldPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
                    await RepopulateViewModelForError(model, user);
                    return View(model);
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Your profile has been updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            if (profilePicture == null || profilePicture.Length == 0) return BadRequest(new { message = "No file uploaded or file is empty." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found.");

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl)) DeleteFile(user.ProfilePictureUrl);

            string newProfilePictureUrl = await UploadFile(profilePicture, "avatars");
            user.ProfilePictureUrl = newProfilePictureUrl;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) return Ok(new { newImageUrl = newProfilePictureUrl });

            return BadRequest(new { message = "Failed to update profile picture in the database." });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        private async Task PopulateProfileViewModel(ProfileViewModel model, ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            model.Username = user.UserName;
            model.Email = user.Email;
            model.FullName = user.FullName;
            model.PhoneNumber = user.PhoneNumber;
            model.ProfilePictureUrl = user.ProfilePictureUrl;
            model.Roles = userRoles.ToList();
            model.Messages = await _context.ContactMessages.Where(m => m.ApplicationUserId == user.Id).OrderByDescending(m => m.SubmittedAt).Select(m => new ContactMessageDto { SourcePage = m.SourcePage, Message = m.Message, SubmittedAt = m.SubmittedAt }).ToListAsync();
            model.Orders = await _context.Orders.Where(o => o.ApplicationUserId == user.Id).OrderByDescending(o => o.OrderDate).Select(o => new OrderSummaryDto { OrderId = o.Id, OrderDate = o.OrderDate, TotalAmount = o.TotalAmount, OrderStatus = o.OrderStatus }).ToListAsync();
            model.WishlistItems = await _context.Wishlists.Where(w => w.ApplicationUserId == user.Id).Include(w => w.Product).ThenInclude(p => p.Stock).Include(w => w.Product.Reviews).Select(w => w.Product).ToListAsync();
        }

        private async Task RepopulateViewModelForError(ProfileViewModel model, ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            model.Username = user.UserName;
            model.ProfilePictureUrl = user.ProfilePictureUrl;
            model.Roles = userRoles.ToList();
            model.Messages = await _context.ContactMessages.Where(m => m.ApplicationUserId == user.Id).OrderByDescending(m => m.SubmittedAt).Select(m => new ContactMessageDto { SourcePage = m.SourcePage, Message = m.Message, SubmittedAt = m.SubmittedAt }).ToListAsync();
            model.Orders = await _context.Orders.Where(o => o.ApplicationUserId == user.Id).OrderByDescending(o => o.OrderDate).Select(o => new OrderSummaryDto { OrderId = o.Id, OrderDate = o.OrderDate, TotalAmount = o.TotalAmount, OrderStatus = o.OrderStatus }).ToListAsync();
            model.WishlistItems = await _context.Wishlists.Where(w => w.ApplicationUserId == user.Id).Include(w => w.Product).ThenInclude(p => p.Stock).Include(w => w.Product.Reviews).Select(w => w.Product).ToListAsync();
        }

        private IActionResult RedirectToLocal(string returnUrl) => Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");

        private async Task<string> UploadFile(IFormFile file, string subfolder)
        {
            if (file == null) return null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subfolder);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(fileStream); }
            return $"/uploads/{subfolder}/{uniqueFileName}";
        }

        private void DeleteFile(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) { System.IO.File.Delete(filePath); }
        }

        private void AddIdentityErrors(IdentityResult result, string lang)
        {
            foreach (var error in result.Errors)
            {
                string errorMessage = error.Description;
                if (lang == "ar")
                {
                    if (error.Code.Contains("PasswordTooShort")) errorMessage = $"كلمة المرور يجب أن لا تقل عن {error.Description.Split(' ').LastOrDefault()} أحرف.";
                    else if (error.Code.Contains("DuplicateUserName")) errorMessage = $"اسم المستخدم '{error.Description.Split('\'').ElementAtOrDefault(1)}' مسجل بالفعل.";
                    else if (error.Code.Contains("DuplicateEmail")) errorMessage = $"البريد الإلكتروني '{error.Description.Split('\'').ElementAtOrDefault(1)}' مسجل بالفعل.";
                    else if (error.Code.Contains("InvalidUserName")) errorMessage = $"اسم المستخدم '{error.Description.Split('\'').ElementAtOrDefault(1)}' غير صالح، يمكن أن يحتوي فقط على حروف وأرقام.";
                    else if (error.Code.Contains("PasswordRequiresNonAlphanumeric")) errorMessage = "كلمة المرور يجب أن تحتوي على رمز واحد على الأقل (مثل @, #, $).";
                    else if (error.Code.Contains("PasswordRequiresDigit")) errorMessage = "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل ('0'-'9').";
                    else if (error.Code.Contains("PasswordRequiresUpper")) errorMessage = "كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل ('A'-'Z').";
                }
                ModelState.AddModelError(string.Empty, errorMessage);
            }
        }
    }
}
