using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioGC.Models
{
    /// <summary>
    /// Represents a purchasable relaxation package containing media content.
    /// This is now a standalone entity, separate from Products.
    /// </summary>
    public class RelaxationPackage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "English title is required.")]
        [StringLength(200)]
        public string TitleEn { get; set; }

        [Required(ErrorMessage = "Arabic title is required.")]
        [StringLength(200)]
        public string TitleAr { get; set; }

        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }

        public string ImageUrl { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "A category must be selected.")]
        public int? CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // Navigation properties for associated media and user access
        public virtual ICollection<RelaxationVideo> Videos { get; set; } = new List<RelaxationVideo>();
        public virtual ICollection<RelaxationAudio> Audios { get; set; } = new List<RelaxationAudio>();
        public virtual ICollection<UserRelaxationPackage> PurchasedByUsers { get; set; } = new List<UserRelaxationPackage>();
    }
}
