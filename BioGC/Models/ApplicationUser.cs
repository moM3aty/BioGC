using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ContactMessage> ContactMessages { get; set; } = new List<ContactMessage>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    }
}
