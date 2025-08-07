using BioGC.Data;
using BioGC.Models;
using BioGC.Services;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly StripeSettings _stripeSettings;
        private readonly NotificationService _notificationService;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IOptions<StripeSettings> stripeSettings, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _stripeSettings = stripeSettings.Value;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            // This logic remains the same
            var user = await _userManager.GetUserAsync(User);
            var shippingZones = await _context.ShippingZones.Select(z => new SelectListItem { Value = z.Id.ToString(), Text = $"{z.ZoneNameEn} / {z.ZoneNameAr} (+${z.ShippingCost:F2})" }).ToListAsync();
            var viewModel = new CheckoutViewModel { FullName = user.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber, StripePublishableKey = _stripeSettings.PublishableKey, ShippingZones = shippingZones };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest payload)
        {
            // This logic remains the same
            if (payload?.CartItems == null || !payload.CartItems.Any()) return BadRequest(new { error = "Cart is empty." });
            var user = await _userManager.GetUserAsync(User);
            decimal itemsTotal = 0;
            var orderItems = new List<OrderItem>();
            foreach (var item in payload.CartItems)
            {
                var product = await _context.Products.FindAsync(item.Id);
                if (product == null) return BadRequest(new { error = $"Product '{item.NameEn}' is no longer available." });
                itemsTotal += product.PriceAfterDiscount * item.Quantity;
                orderItems.Add(new OrderItem { ProductId = item.Id, Quantity = item.Quantity, Price = product.PriceAfterDiscount });
            }
            decimal totalAmount = itemsTotal;
            decimal shippingCost = 0;
            if (payload.ShippingZoneId > 0)
            {
                var shippingZone = await _context.ShippingZones.FindAsync(payload.ShippingZoneId);
                if (shippingZone != null) { shippingCost = shippingZone.ShippingCost; totalAmount += shippingCost; }
            }
            var order = new Order { ApplicationUserId = user.Id, OrderDate = System.DateTime.UtcNow, ShippingAddress = payload.ShippingAddress, ShippingZoneId = payload.ShippingZoneId > 0 ? payload.ShippingZoneId : null, ShippingCost = shippingCost, TotalAmount = totalAmount, OrderStatus = "Pending Payment", OrderItems = orderItems };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            var domain = $"{Request.Scheme}://{Request.Host}";
            var lineItems = order.OrderItems.Select(item => new SessionLineItemOptions { PriceData = new SessionLineItemPriceDataOptions { UnitAmountDecimal = item.Price * 100, Currency = "usd", ProductData = new SessionLineItemPriceDataProductDataOptions { Name = _context.Products.Find(item.ProductId).NameEn }, }, Quantity = item.Quantity, }).ToList();
            if (order.ShippingCost > 0) { lineItems.Add(new SessionLineItemOptions { PriceData = new SessionLineItemPriceDataOptions { UnitAmountDecimal = order.ShippingCost * 100, Currency = "usd", ProductData = new SessionLineItemPriceDataProductDataOptions { Name = "Shipping Cost" }, }, Quantity = 1, }); }
            var options = new SessionCreateOptions { PaymentMethodTypes = new List<string> { "card" }, LineItems = lineItems, Mode = "payment", SuccessUrl = domain + $"/Checkout/OrderConfirmation?session_id={{CHECKOUT_SESSION_ID}}", CancelUrl = domain + "/Checkout/OrderCancelled", Metadata = new Dictionary<string, string> { { "order_id", order.Id.ToString() } } };
            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            order.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();
            return Ok(new { sessionId = session.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Service(int productId)
        {
            // This logic remains the same
            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.Id != 1) { return NotFound(); }
            var user = await _userManager.GetUserAsync(User);
            var domain = $"{Request.Scheme}://{Request.Host}";
            var order = new Order { ApplicationUserId = user.Id, OrderDate = System.DateTime.UtcNow, ShippingAddress = "N/A (Service)", TotalAmount = product.PriceAfterDiscount, OrderStatus = "Pending Payment", OrderItems = new List<OrderItem> { new OrderItem { ProductId = product.Id, Quantity = 1, Price = product.PriceAfterDiscount } } };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            var options = new SessionCreateOptions { PaymentMethodTypes = new List<string> { "card" }, LineItems = new List<SessionLineItemOptions> { new SessionLineItemOptions { PriceData = new SessionLineItemPriceDataOptions { UnitAmount = (long)(product.PriceAfterDiscount * 100), Currency = "usd", ProductData = new SessionLineItemPriceDataProductDataOptions { Name = $"Access: {product.NameEn}", }, }, Quantity = 1, }, }, Mode = "payment", SuccessUrl = domain + "/Checkout/OrderConfirmation?session_id={CHECKOUT_SESSION_ID}", CancelUrl = domain + "/Checkout/OrderCancelled", Metadata = new Dictionary<string, string> { { "order_id", order.Id.ToString() } } };
            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            order.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();
            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(string session_id)
        {
            var sessionService = new SessionService();
            Session session = await sessionService.GetAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                var orderIdStr = session.Metadata["order_id"];
                if (int.TryParse(orderIdStr, out var orderId))
                {
                    var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
                    if (order != null && order.OrderStatus == "Pending Payment")
                    {
                        bool isRelaxationSubscription = order.OrderItems.Any(oi => oi.ProductId == 1);
                        if (isRelaxationSubscription)
                        {
                            // --- FIX: Logic to handle re-subscription ---
                            var existingSubscription = await _context.RelaxationSubscriptions
                                .FirstOrDefaultAsync(s => s.ApplicationUserId == order.ApplicationUserId);

                            if (existingSubscription != null)
                            {
                                // User has a previous (likely cancelled) subscription. Reactivate it.
                                existingSubscription.Status = "Pending Approval";
                                existingSubscription.OrderId = order.Id; // Link to the new payment order
                                existingSubscription.SubscriptionDate = System.DateTime.UtcNow;
                                _context.RelaxationSubscriptions.Update(existingSubscription);
                            }
                            else
                            {
                                // This is a brand new subscriber.
                                var subscription = new RelaxationSubscription
                                {
                                    ApplicationUserId = order.ApplicationUserId,
                                    OrderId = order.Id,
                                    SubscriptionDate = System.DateTime.UtcNow,
                                    Status = "Pending Approval"
                                };
                                _context.RelaxationSubscriptions.Add(subscription);
                            }

                            order.OrderStatus = "Subscription";
                            await _notificationService.SendNotificationToAdminsAsync("New Relaxation Service purchase requires approval.", "اشتراك جديد في خدمة الاسترخاء يتطلب الموافقة.", "/Admin/Subscriptions");
                        }
                        else
                        {
                            order.OrderStatus = "Processing";
                            await _notificationService.SendNotificationToAdminsAsync($"New order #{order.Id} has been placed.", $"تم وضع طلب جديد برقم #{order.Id}.", $"/Admin/Orders/Details/{order.Id}");
                        }

                        await _context.SaveChangesAsync();
                        ViewBag.OrderId = order.Id;
                    }
                }
            }

            return View("ThankYou");
        }

        public IActionResult OrderCancelled()
        {
            return View();
        }
    }


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
}
