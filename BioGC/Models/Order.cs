using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BioGC.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; } 

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ShippingCost { get; set; } 

        public string ShippingAddress { get; set; }
        public string OrderStatus { get; set; }

        public string? StripeSessionId { get; set; }

        public int? ShippingZoneId { get; set; } 
        public virtual ShippingZone ShippingZone { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
