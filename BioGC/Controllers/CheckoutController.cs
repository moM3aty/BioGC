using BioGC.Data;
using BioGC.Models;
using BioGC.Services;
using BioGC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BioGC.Controllers
{
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly PayPalService _paypalService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            NotificationService notificationService,
            PayPalService paypalService,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _paypalService = paypalService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var shippingZones = await _context.ShippingZones.Select(z => new SelectListItem { Value = z.Id.ToString(), Text = $"{z.ZoneNameEn} / {z.ZoneNameAr} (+${z.ShippingCost:F2})" }).ToListAsync();
            var payPalSettings = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<PayPalSettings>>();
            var viewModel = new CheckoutViewModel { FullName = user.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber, PayPalClientId = payPalSettings.Value.ClientId, ShippingZones = shippingZones };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Service(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || product.Id != 1) { return NotFound(); }

            var user = await _userManager.GetUserAsync(User);
            var order = new Order
            {
                ApplicationUserId = user.Id,
                OrderDate = System.DateTime.UtcNow,
                ShippingAddress = "N/A (Service)",
                TotalAmount = product.PriceAfterDiscount,
                OrderStatus = "Pending Payment",
                OrderItems = new List<OrderItem> { new OrderItem { ProductId = product.Id, Quantity = 1, Price = product.PriceAfterDiscount } }
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            try
            {
                var items = new List<Dictionary<string, object>>
                {
                    new()
                    {
                        { "name", product.NameEn },
                        { "quantity", "1" },
                        { "unit_amount", new Dictionary<string, string> { { "currency_code", "USD" }, { "value", product.PriceAfterDiscount.ToString("F2") } } }
                    }
                };

                var domain = $"{Request.Scheme}://{Request.Host}";
                var returnUrl = $"{domain}/Checkout/OrderConfirmation?orderId={order.Id}";
                var cancelUrl = $"{domain}/Checkout/OrderCancelled";

                var paypalOrder = await _paypalService.CreateOrderAsync(order.TotalAmount, order.TotalAmount, 0m, items, returnUrl, cancelUrl);
                order.PaymentGatewayId = paypalOrder.GetProperty("id").GetString();
                await _context.SaveChangesAsync();

                return RedirectToAction("Payment", new { orderId = order.Id, paypalOrderId = order.PaymentGatewayId });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order for Service ID {ProductId}", productId);
                order.OrderStatus = "Failed";
                await _context.SaveChangesAsync();
                return View("OrderCancelled");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequest payload)
        {
            _logger.LogInformation("--- CreateOrder endpoint hit. ---");

            if (payload == null)
            {
                _logger.LogError(">>> CRITICAL: The payload received in CreateOrder was NULL.");
                return BadRequest(new { error = "Payload cannot be null." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                var errorString = string.Join(" | ", errors);
                _logger.LogError(">>> CRITICAL: CreateOrder model validation failed. Errors: {Errors}", errorString);
                return BadRequest(new { error = "Invalid data provided.", details = errors });
            }

            _logger.LogInformation("Payload and ModelState are valid. Proceeding to create order.");

            var user = await _userManager.GetUserAsync(User);
            decimal itemsSubtotal = 0;
            var orderItems = new List<OrderItem>();
            var paypalItems = new List<Dictionary<string, object>>();

            foreach (var item in payload.CartItems)
            {
                var product = await _context.Products.FindAsync(item.Id);
                if (product == null) return BadRequest(new { error = $"Product '{item.NameEn}' is no longer available." });

                itemsSubtotal += product.PriceAfterDiscount * item.Quantity;
                orderItems.Add(new OrderItem { ProductId = item.Id, Quantity = item.Quantity, Price = product.PriceAfterDiscount });
                paypalItems.Add(new Dictionary<string, object>
                {
                    { "name", product.NameEn },
                    { "quantity", item.Quantity.ToString() },
                    { "unit_amount", new Dictionary<string, string> { { "currency_code", "USD" }, { "value", product.PriceAfterDiscount.ToString("F2") } } }
                });
            }

            decimal shippingCost = 0;
            if (payload.ShippingZoneId > 0)
            {
                var shippingZone = await _context.ShippingZones.FindAsync(payload.ShippingZoneId);
                if (shippingZone != null) { shippingCost = shippingZone.ShippingCost; }
            }
            decimal totalAmount = itemsSubtotal + shippingCost;

            var order = new Order { ApplicationUserId = user.Id, OrderDate = System.DateTime.UtcNow, ShippingAddress = payload.ShippingAddress, ShippingZoneId = payload.ShippingZoneId > 0 ? payload.ShippingZoneId : null, ShippingCost = shippingCost, TotalAmount = totalAmount, OrderStatus = "Pending Payment", OrderItems = orderItems };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            try
            {
                var domain = $"{Request.Scheme}://{Request.Host}";
                var returnUrl = $"{domain}/Checkout/OrderConfirmation?orderId={order.Id}";
                var cancelUrl = $"{domain}/Checkout/OrderCancelled";

                var paypalOrder = await _paypalService.CreateOrderAsync(totalAmount, itemsSubtotal, shippingCost, paypalItems, returnUrl, cancelUrl);
                var paypalOrderId = paypalOrder.GetProperty("id").GetString();
                order.PaymentGatewayId = paypalOrderId;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created PayPal order {PayPalOrderId} for internal order {OrderId}", paypalOrderId, order.Id);
                return Ok(new { orderId = paypalOrderId });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, ">>> CRITICAL: Error calling PayPal service for Cart. Internal Order ID: {OrderId}", order.Id);
                order.OrderStatus = "Failed";
                await _context.SaveChangesAsync();
                return BadRequest(new { error = "Could not create PayPal order on the server." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CaptureOrder([FromBody] CaptureOrderRequest payload)
        {
            try
            {
                var captureResponse = await _paypalService.CaptureOrderAsync(payload.PayPalOrderId);
                var status = captureResponse.GetProperty("status").GetString();

                if (status == "COMPLETED")
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.PaymentGatewayId == payload.PayPalOrderId);

                    if (order != null && order.OrderStatus == "Pending Payment")
                    {
                        bool isRelaxationSubscription = order.OrderItems.Any(oi => oi.ProductId == 1);
                        if (isRelaxationSubscription)
                        {
                            var existingSubscription = await _context.RelaxationSubscriptions.FirstOrDefaultAsync(s => s.ApplicationUserId == order.ApplicationUserId);
                            if (existingSubscription != null)
                            {
                                existingSubscription.Status = "Pending Approval";
                                existingSubscription.OrderId = order.Id;
                                existingSubscription.SubscriptionDate = System.DateTime.UtcNow;
                                _context.RelaxationSubscriptions.Update(existingSubscription);
                            }
                            else
                            {
                                _context.RelaxationSubscriptions.Add(new RelaxationSubscription { ApplicationUserId = order.ApplicationUserId, OrderId = order.Id, SubscriptionDate = System.DateTime.UtcNow, Status = "Pending Approval" });
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
                        return Ok(new { success = true, orderId = order.Id });
                    }
                }
                _logger.LogWarning("Payment capture status was not COMPLETED for PayPal Order ID {PayPalOrderId}. Status: {Status}", payload.PayPalOrderId, status);
                return BadRequest(new { success = false, message = "Payment not completed." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order for PayPal Order ID {PayPalOrderId}", payload.PayPalOrderId);
                return StatusCode(500, new { success = false, message = "An error occurred while capturing payment." });
            }
        }

        [HttpGet]
        public IActionResult OrderConfirmation(int? orderId)
        {
            if (orderId.HasValue)
            {
                ViewBag.OrderId = orderId.Value;
            }
            return View("ThankYou");
        }

        public IActionResult OrderCancelled() => View();

        public async Task<IActionResult> Payment(int orderId, string paypalOrderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            var payPalSettings = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<PayPalSettings>>();
            ViewBag.ClientId = payPalSettings.Value.ClientId;
            ViewBag.PayPalOrderId = paypalOrderId;
            ViewBag.OrderId = order.Id;

            return View(order);
        }
    }

    public class CheckoutRequest { public string ShippingAddress { get; set; } public int ShippingZoneId { get; set; } public List<CartItemDto> CartItems { get; set; } }
    public class CartItemDto { public int Id { get; set; } public int Quantity { get; set; } public string NameEn { get; set; } public decimal Price { get; set; } }
    public class CaptureOrderRequest { public string PayPalOrderId { get; set; } public int InternalOrderId { get; set; } }
}

