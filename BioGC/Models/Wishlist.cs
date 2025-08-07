using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class Wishlist
    {
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}
