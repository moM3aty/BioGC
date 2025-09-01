using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BioGC.Services
{
    public class PayPalService
    {
        private readonly PayPalSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(IOptions<PayPalSettings> settings, ILogger<PayPalService> logger)
        {
            _settings = settings.Value;
            _httpClient = new HttpClient();
            _logger = logger;
        }

        private async Task<string> GetAccessToken()
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.Secret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token");
            request.Headers.Add("Authorization", $"Basic {auth}");
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("PayPal GetAccessToken failed with status code {StatusCode}. Response: {Response}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<JsonElement> CreateOrderAsync(decimal totalAmount, decimal itemsSubtotal, decimal shippingCost, List<Dictionary<string, object>> items, string returnUrl, string cancelUrl)
        {
            var accessToken = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var payload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = "USD",
                            value = totalAmount.ToString("F2"),
                            breakdown = new
                            {
                                item_total = new { currency_code = "USD", value = itemsSubtotal.ToString("F2") },
                                shipping = new { currency_code = "USD", value = shippingCost.ToString("F2") }
                            }
                        },
                        items
                    }
                },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v2/checkout/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create PayPal order. Status: {StatusCode}, Response: {ResponseContent}", response.StatusCode, responseContent);
                throw new HttpRequestException($"PayPal API error: {responseContent}");
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement;
        }

        public async Task<JsonElement> CaptureOrderAsync(string orderId)
        {
            var accessToken = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v2/checkout/orders/{orderId}/capture", content);

            var responseContent = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to capture PayPal order {OrderId}. Status: {StatusCode}, Response: {ResponseContent}", orderId, response.StatusCode, responseContent);
                throw new HttpRequestException($"PayPal API error: {responseContent}");
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            return jsonDoc.RootElement;
        }
    }
}

