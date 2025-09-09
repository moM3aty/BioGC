using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    /// <summary>
    /// Represents a category for products or relaxation packages.
    /// </summary>
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string NameEn { get; set; }

        [Required]
        [StringLength(100)]
        public string NameAr { get; set; }

        public int? ParentCategoryId { get; set; }
        public virtual Category? ParentCategory { get; set; }

        // Navigation Properties
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        // NEW: Navigation property for relaxation packages
        public virtual ICollection<RelaxationPackage> RelaxationPackages { get; set; } = new List<RelaxationPackage>();
    }
}
