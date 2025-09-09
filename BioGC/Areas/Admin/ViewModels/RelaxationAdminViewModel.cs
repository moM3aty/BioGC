using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Areas.Admin.ViewModels
{
    /// <summary>
    /// ViewModel for creating and editing Relaxation Packages in the Admin area.
    /// It no longer references a Product.
    /// </summary>
    public class RelaxationPackageViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "English title is required.")]
        [Display(Name = "Title (English)")]
        public string TitleEn { get; set; }

        [Required(ErrorMessage = "Arabic title is required.")]
        [Display(Name = "Title (Arabic)")]
        public string TitleAr { get; set; }

        [Display(Name = "Description (English)")]
        public string DescriptionEn { get; set; }

        [Display(Name = "Description (Arabic)")]
        public string DescriptionAr { get; set; }

        [Display(Name = "Cover Image")]
        public IFormFile ImageFile { get; set; }
        public string ExistingImageUrl { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 10000.00, ErrorMessage = "Price must be a positive value.")]
        [Display(Name = "Price (USD)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please select a category for the package.")]
        [Display(Name = "Package Category")]
        public int? CategoryId { get; set; }
        public SelectList CategoryList { get; set; }
    }

    /// <summary>
    /// ViewModel for the ManageMedia page.
    /// </summary>
    public class RelaxationMediaViewModel
    {
        public int PackageId { get; set; }
        public string PackageTitle { get; set; }
        public IEnumerable<Models.RelaxationVideo> Videos { get; set; }
        public IEnumerable<Models.RelaxationAudio> Audios { get; set; }

        // For the "Add New" forms
        [Required]
        [Display(Name = "Title")]
        public string NewItemTitle { get; set; }

        [Required]
        [Display(Name = "Bunny.net Library ID")]
        public string NewItemLibraryId { get; set; }

        [Required]
        [Display(Name = "Bunny.net GUID")]
        public string NewItemGuid { get; set; }
    }
}
