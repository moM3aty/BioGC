using BioGC.Models;
using BioGC.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string searchTerm, string role)
        {
            // Start with IQueryable for database-level filtering
            var usersQuery = _userManager.Users.AsQueryable();

            // Apply search filter at the database level
            if (!string.IsNullOrEmpty(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.UserName != null && u.UserName.Contains(searchTerm)) ||
                    (u.FullName != null && u.FullName.Contains(searchTerm)) ||
                    (u.Email != null && u.Email.Contains(searchTerm))
                );
            }

            // Fetch filtered users
            var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Roles = await _userManager.GetRolesAsync(user)
                });
            }

            // Apply role filter in-memory (as roles are managed by Identity and harder to join efficiently in a simple query)
            if (!string.IsNullOrEmpty(role))
            {
                userViewModels = userViewModels.Where(u => u.Roles.Contains(role)).ToList();
            }

            var rolesList = await _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToListAsync();

            var viewModel = new UserIndexViewModel
            {
                Users = userViewModels,
                Roles = new SelectList(rolesList, "Value", "Text", role),
                SearchTerm = searchTerm,
                Role = role
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = new List<RoleViewModel>()
            };

            foreach (var role in await _roleManager.Roles.ToListAsync())
            {
                model.Roles.Add(new RoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                });
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user, model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName));

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            return RedirectToAction("Index");
        }
    }
}
