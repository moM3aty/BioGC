using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }
        public DateTime DatePosted { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; } = "Pending"; 

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
