using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string SourcePage { get; set; } 

        public string? ProductCategory { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public string? ApplicationUserId { get; set; } 
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}