using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioGC.Models
{
    public class RelaxationContent
    {
        public int Id { get; set; }

        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
        public virtual ICollection<RelaxationVideo> Videos { get; set; } = new List<RelaxationVideo>();
        public virtual ICollection<RelaxationAudio> Audios { get; set; } = new List<RelaxationAudio>();
    }
}
