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
using Microsoft.Extensions.Options;
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
        private readonly NotificationService _notificationService;
        private readonly PayPalService _paypalService;
        private readonly ILogger<CheckoutController> _logger;
        private readonly AppSettings _appSettings;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService, PayPalService paypalService, ILogger<CheckoutController> logger, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _paypalService = paypalService;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var shippingZones = await _context.ShippingZones.Select(z => new SelectListItem { Value = z.Id.ToString(), Text = $"{z.ZoneNameEn} / {z.ZoneNameAr} (+${z.ShippingCost:F2})" }).ToListAsync();
            var payPalSettings = HttpContext.RequestServices.GetRequiredService<IOptions<PayPalSettings>>();
            var viewModel = new CheckoutViewModel { FullName = user.FullName, Email = user.Email, PhoneNumber = user.PhoneNumber, PayPalClientId = payPalSettings.Value.ClientId, ShippingZones = shippingZones };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PurchasePackage(int id)
        {
            var package = await _context.RelaxationPackages.FindAsync(id);
            if (package == null) return NotFound();
            var payPalSettings = HttpContext.RequestServices.GetRequiredService<IOptions<PayPalSettings>>();

            var viewModel = new PurchasePackageViewModel
            {
                PackageId = package.Id,
                TitleEn = package.TitleEn,
                TitleAr = package.TitleAr,
                Price = package.Price,
                PayPalClientId = payPalSettings.Value.ClientId
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePackageOrder([FromForm] PurchasePackageRequest payload)
        {
            if (payload == null || payload.PackageId <= 0)
            {
                return BadRequest(new { error = "Invalid package ID." });
            }

            var package = await _context.RelaxationPackages.FindAsync(payload.PackageId);
            if (package == null) return NotFound(new { error = "Package not found." });

            var user = await _userManager.GetUserAsync(User);
            int relaxationServiceProductId = _appSettings.RelaxationServiceProductId;

            var order = new Order
            {
                ApplicationUserId = user.Id,
                OrderDate = System.DateTime.UtcNow,
                ShippingAddress = "N/A (Digital Service)",
                TotalAmount = package.Price,
                OrderStatus = "Pending Payment",
                OrderItems = new List<OrderItem> { new OrderItem { ProductId = relaxationServiceProductId, Quantity = 1, Price = package.Price } }
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var subscription = new RelaxationSubscription
            {
                ApplicationUserId = user.Id,
                OrderId = order.Id,
                RelaxationPackageId = package.Id,
                SubscriptionDate = System.DateTime.UtcNow,
                Status = "Pending Payment"
            };
            _context.RelaxationSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            try
            {
                var items = new List<Dictionary<string, object>> { new() { { "name", package.TitleEn }, { "quantity", "1" }, { "unit_amount", new Dictionary<string, string> { { "currency_code", "USD" }, { "value", package.Price.ToString("F2") } } } } };
                var domain = $"{Request.Scheme}://{Request.Host}";
                var returnUrl = $"{domain}/Checkout/OrderConfirmation?orderId={order.Id}";
                var cancelUrl = $"{domain}/Checkout/OrderCancelled";
                var paypalOrder = await _paypalService.CreateOrderAsync(order.TotalAmount, order.TotalAmount, 0m, items, returnUrl, cancelUrl);
                var paypalOrderId = paypalOrder.GetProperty("id").GetString();
                order.PaymentGatewayId = paypalOrderId;
                await _context.SaveChangesAsync();
                return Ok(new { orderId = paypalOrderId });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order for Package ID {PackageId}", payload.PackageId);
                order.OrderStatus = "Failed";
                subscription.Status = "Failed";
                await _context.SaveChangesAsync();
                return BadRequest(new { error = "Could not create PayPal order." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequest payload)
        {
            if (payload == null || !ModelState.IsValid) return BadRequest(new { error = "Invalid data provided." });

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
                paypalItems.Add(new Dictionary<string, object> { { "name", product.NameEn }, { "quantity", item.Quantity.ToString() }, { "unit_amount", new Dictionary<string, string> { { "currency_code", "USD" }, { "value", product.PriceAfterDiscount.ToString("F2") } } } });
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
                return Ok(new { orderId = paypalOrderId });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error calling PayPal service for Cart. Internal Order ID: {OrderId}", order.Id);
                order.OrderStatus = "Failed";
                await _context.SaveChangesAsync();
                return BadRequest(new { error = "Could not create PayPal order on the server." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureOrder([FromForm] CaptureOrderRequest payload)
        {
            try
            {
                var captureResponse = await _paypalService.CaptureOrderAsync(payload.PayPalOrderId);
                var status = captureResponse.GetProperty("status").GetString();

                if (status != "COMPLETED")
                {
                    _logger.LogWarning("Payment capture was not COMPLETED for PayPal Order ID {PayPalOrderId}.", payload.PayPalOrderId);
                    return BadRequest(new { success = false, message = "Payment not completed." });
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.PaymentGatewayId == payload.PayPalOrderId);
                if (order == null || order.OrderStatus != "Pending Payment")
                {
                    _logger.LogError("Could not find a valid pending order for PayPal ID {PayPalOrderId}", payload.PayPalOrderId);
                    return BadRequest(new { success = false, message = "Order not found or already processed." });
                }

                var subscription = await _context.RelaxationSubscriptions.FirstOrDefaultAsync(s => s.OrderId == order.Id);
                if (subscription != null)
                {
                    order.OrderStatus = "Awaiting Approval";
                    subscription.Status = "Pending Approval";
                    await _notificationService.SendNotificationToAdminsAsync($"New Relaxation Package purchase requires approval (Order #{order.Id}).", $"اشتراك جديد في باقة استرخاء يتطلب الموافقة (طلب رقم #{order.Id}).", "/Admin/Subscriptions");
                }
                else
                {
                    order.OrderStatus = "Processing";
                    await _notificationService.SendNotificationToAdminsAsync($"New order #{order.Id} has been placed.", $"تم وضع طلب جديد برقم #{order.Id}.", $"/Admin/Orders/Details/{order.Id}");
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, orderId = order.Id });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {PayPalOrderId}", payload.PayPalOrderId);
                return StatusCode(500, new { success = false, message = "An error occurred." });
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
    }
}

