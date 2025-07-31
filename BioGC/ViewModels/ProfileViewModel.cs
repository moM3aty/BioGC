using BioGC.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BioGC.ViewModels
{
    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string ProfilePictureUrl { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string StatusMessage { get; set; }

        public List<string> Roles { get; set; } = new List<string>();

        public List<ContactMessageDto> Messages { get; set; } = new List<ContactMessageDto>();
        public List<OrderSummaryDto> Orders { get; set; } = new List<OrderSummaryDto>();
        public List<Product> WishlistItems { get; set; } = new List<Product>();
    }

    public class ContactMessageDto
    {
        public string SourcePage { get; set; }
        public string Message { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
    }
}
