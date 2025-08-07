using System;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    /// <summary>
    /// Represents a user's subscription to the relaxation service.
    /// </summary>
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

        public DateTime SubscriptionDate { get; set; }

        [Required]
        public string Status { get; set; }
    }
}
