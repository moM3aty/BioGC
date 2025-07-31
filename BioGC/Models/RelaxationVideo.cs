using System.ComponentModel.DataAnnotations;

namespace BioGC.Models
{
    public class RelaxationVideo
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Bunny.net Library ID")]
        public int LibraryId { get; set; }

        [Required]
        [Display(Name = "Bunny.net Video GUID")]
        public string VideoGuid { get; set; }

        public int RelaxationContentId { get; set; }
        public virtual RelaxationContent RelaxationContent { get; set; }
    }
}
