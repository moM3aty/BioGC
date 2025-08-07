using BioGC.Models; 
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioGC.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [Display(Name = "الرسالة (العربية)")]
        public string MessageAr { get; set; }

        [Required]
        [Display(Name = "Message (English)")]
        public string MessageEn { get; set; }

        public string Url { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
