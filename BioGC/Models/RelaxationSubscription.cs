using System;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class RelaxationSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        [Required]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        [Required]
        public int RelaxationPackageId { get; set; }
        public virtual RelaxationPackage RelaxationPackage { get; set; }

        public DateTime SubscriptionDate { get; set; }

        [Required]
        public string Status { get; set; } // e.g., "Pending Payment", "Pending Approval", "Approved", "Cancelled", "Failed"
    }
}
