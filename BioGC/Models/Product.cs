using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioGC.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string NameEn { get; set; }

        [Required]
        [StringLength(200)]
        public string NameAr { get; set; }

        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PriceBeforeDiscount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal PriceAfterDiscount { get; set; }

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
        public virtual Stock Stock { get; set; }
        public bool IsListed { get; set; } = true;

        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }
}
