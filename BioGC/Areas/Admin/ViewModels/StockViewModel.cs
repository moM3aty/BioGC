using System.ComponentModel.DataAnnotations;

namespace BioGC.ViewModels
{
    public class StockViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be a negative number.")]
        [Display(Name = "Quantity in Stock")]
        public int Quantity { get; set; }
    }
}
