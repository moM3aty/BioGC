using BioGC.Models;
using System.ComponentModel.DataAnnotations;

namespace BioGC.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public bool CanUserReview { get; set; } 

        [Range(1, 5, ErrorMessage = "Please select a rating.")]
        public int NewReviewRating { get; set; }

        [MaxLength(500)]
        public string NewReviewComment { get; set; }
        public List<Product> RelatedProducts { get; set; }

    }
}