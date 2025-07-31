using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Areas.Admin.ViewModels
{
    public class ProductViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name (English)")]
        public string NameEn { get; set; }

        [Required]
        [Display(Name = "Name (Arabic)")]
        public string NameAr { get; set; }

        [Display(Name = "Description (English)")]
        public string DescriptionEn { get; set; }

        [Display(Name = "Description (Arabic)")]
        public string DescriptionAr { get; set; }

        [Required]
        [Display(Name = "Price Before Discount")]
        [Range(0.01, 100000.00)]
        public decimal PriceBeforeDiscount { get; set; }

        [Required]
        [Display(Name = "Price After Discount")]
        [Range(0.01, 100000.00)]
        public decimal PriceAfterDiscount { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be a negative number.")]
        [Display(Name = "Initial Quantity in Stock")]
        public int Quantity { get; set; }

        public IEnumerable<SelectListItem> CategoryList { get; set; }
    }
}