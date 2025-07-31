using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class RelaxationAudio
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Bunny.net Library ID")]
        public int LibraryId { get; set; }

        [Required]
        [Display(Name = "Bunny.net Audio GUID")]
        public string AudioGuid { get; set; }

        public int RelaxationContentId { get; set; }
        public virtual RelaxationContent RelaxationContent { get; set; }
    }
}
