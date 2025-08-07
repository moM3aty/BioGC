using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioGC.Models
{
    public class ShippingZone
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ZoneNameEn { get; set; }

        [Required]
        [StringLength(100)]
        public string ZoneNameAr { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ShippingCost { get; set; }
    }
}