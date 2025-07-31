using System.ComponentModel.DataAnnotations;

namespace BioGC.ViewModels
{
    public class ContactViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Request { get; set; } 

        [Required]
        public string Message { get; set; }

        [Required]
        public string Service { get; set; } 
    }
}
