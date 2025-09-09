using System.Collections.Generic;

namespace BioGC.ViewModels
{
    // DTO for the main checkout page (shopping cart)
    public class CheckoutRequest
    {
        public string ShippingAddress { get; set; }
        public int ShippingZoneId { get; set; }
        public List<CartItemDto> CartItems { get; set; }
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public string NameEn { get; set; }
        public decimal Price { get; set; }
    }

    // DTO for capturing the order from PayPal
    public class CaptureOrderRequest
    {
        public string PayPalOrderId { get; set; }
    }

    // DTO for creating a package purchase order
    public class PurchasePackageRequest
    {
        public int PackageId { get; set; }
    }
}
