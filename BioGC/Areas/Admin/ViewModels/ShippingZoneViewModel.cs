using BioGC.Models;
using System.ComponentModel.DataAnnotations;

namespace BioGC.Areas.Admin.ViewModels
{
    public class ShippingZoneViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Zone Name (English)")]
        public string ZoneNameEn { get; set; }

        [Required]
        [Display(Name = "Zone Name (Arabic)")]
        public string ZoneNameAr { get; set; }

        [Required]
        [Range(0, 10000)]
        [Display(Name = "Shipping Cost")]
        public decimal ShippingCost { get; set; }
    }
    public class ShippingZoneIndexViewModel
    {
        public IEnumerable<ShippingZone> ShippingZones { get; set; }
        public string SearchTerm { get; set; }
    }
}