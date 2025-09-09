using BioGC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Ensure database is created and migrated
            await context.Database.MigrateAsync();

            // Seed Roles
            string[] roleNames = { "Admin", "Customer", "PremiumUser" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            if (await userManager.FindByEmailAsync("admin@biogc.com") == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@biogc.com",
                    FullName = "Admin User",
                    EmailConfirmed = true,
                };
                var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // Seed a parent category for Relaxation Packages
            var relaxationCategory = await context.Categories.FirstOrDefaultAsync(c => c.NameEn == "Relaxation Programs");
            if (relaxationCategory == null)
            {
                relaxationCategory = new Category { NameEn = "Relaxation Programs", NameAr = "برامج الاسترخاء" };
                context.Categories.Add(relaxationCategory);
                await context.SaveChangesAsync();
            }
        }
    }
}
