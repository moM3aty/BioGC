using BioGC.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Areas.Admin.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name (English)")]
        public string NameEn { get; set; }

        [Required]
        [Display(Name = "Name (Arabic)")]
        public string NameAr { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }

        public IEnumerable<SelectListItem> ParentCategoryList { get; set; }
    }

    public class CategoryApiViewModel
    {
        public int Id { get; set; }

        [Required]
        public string NameEn { get; set; }

        [Required]
        public string NameAr { get; set; }
    }
    public class SubCategoryApiViewModel
    {
        public int Id { get; set; }

        [Required]
        public string NameEn { get; set; }

        [Required]
        public string NameAr { get; set; }

        [Required(ErrorMessage = "Parent category is required.")]
        public int ParentCategoryId { get; set; }
    }
    // NEW ViewModel for the Index page to support filtering
    public class CategoryIndexViewModel
    {
        public IEnumerable<Category> ParentCategories { get; set; }
        public string SearchTerm { get; set; }
    }
}