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

            // Ensure database is created
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

            // Seed Relaxation Service Product
            await SeedRelaxationProduct(context, configuration);
        }

        private static async Task SeedRelaxationProduct(ApplicationDbContext context, IConfiguration configuration)
        {
            var productId = configuration.GetValue<int>("AppSettings:RelaxationServiceProductId");
            if (productId == 0) return; 

            // Check if the product already exists
            if (!await context.Products.AnyAsync(p => p.Id == productId))
            {
                // Ensure a category exists for it
                var category = await context.Categories.FirstOrDefaultAsync(c => c.NameEn == "Digital Services");
                if (category == null)
                {
                    category = new Category { NameEn = "Digital Services", NameAr = "خدمات رقمية" };
                    context.Categories.Add(category);
                    await context.SaveChangesAsync();
                }

                // Create the new product
                var relaxationProduct = new Product
                {
                    Id = productId,
                    NameEn = "Relaxation Content Access",
                    NameAr = "اشتراك محتوى الاسترخاء",
                    DescriptionEn = "Lifetime access to our exclusive library of relaxation videos and audio.",
                    DescriptionAr = "وصول دائم لمكتبتنا الحصرية من فيديوهات وصوتيات الاسترخاء.",
                    ImageUrl = "default-service.jpg", 
                    PriceBeforeDiscount = 10.00m,
                    PriceAfterDiscount = 10.00m,
                    CategoryId = category.Id,
                    IsListed = false,
                    Stock = new Stock { Quantity = 999999 } 
                };

                await context.Database.OpenConnectionAsync();
                try
                {
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Products ON");
                    context.Products.Add(relaxationProduct);
                    await context.SaveChangesAsync();
                    await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Products OFF");
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }
        }
    }
}
