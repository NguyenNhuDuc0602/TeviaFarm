using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeviaFarm.Models;

namespace TeviaFarm.Services
{

    public class GhtkService : IGhtkService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GhtkService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<decimal?> CalculateShippingFeeAsync(string toProvince, string toDistrict, double weightKg, decimal value)
        {
            var token = _configuration["GHTK:Token"];
            var fromProvince = _configuration["GHTK:PickProvince"] ?? "Hà Nội";
            var fromDistrict = _configuration["GHTK:PickDistrict"] ?? "Thạch Thất";

            var query = $"services/shipment/fee" +
                        $"?pick_province={WebUtility.UrlEncode(fromProvince)}" +
                        $"&pick_district={WebUtility.UrlEncode(fromDistrict)}" +
                        $"&province={WebUtility.UrlEncode(toProvince)}" +
                        $"&district={WebUtility.UrlEncode(toDistrict)}" +
                        $"&weight={(int)(weightKg * 1000)}" +
                        $"&value={(int)value}" +
                        $"&transport=road";

            using var request = new HttpRequestMessage(HttpMethod.Get, query);
            request.Headers.Add("Token", token);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("GHTK Fee Error: " + json);
                return null;
            }

            var result = JsonSerializer.Deserialize<GhtkFeeResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || !result.Success || result.Fee == null)
                return null;

            return result.Fee.FeeValue;
        }

        public async Task<GhtkCreateOrderResponse?> CreateOrderAsync(Order order)
        {
            var token = _configuration["GHTK:Token"];
            var pickProvince = _configuration["GHTK:PickProvince"] ?? "Hà Nội";
            var pickDistrict = _configuration["GHTK:PickDistrict"] ?? "Thạch Thất";
            var pickAddress = _configuration["GHTK:PickAddress"] ?? "Địa chỉ lấy hàng";
            var pickName = _configuration["GHTK:PickName"] ?? "Tevia Farm";
            var pickTel = _configuration["GHTK:PickTel"] ?? "0900000000";

            var requestModel = new GhtkCreateOrderRequest
            {
                Order = new GhtkOrderInfo
                {
                    Id = order.OrderId.ToString(),
                    PickName = pickName,
                    PickAddress = pickAddress,
                    PickProvince = pickProvince,
                    PickDistrict = pickDistrict,
                    PickTel = pickTel,

                    Tel = order.ReceiverPhone,
                    Name = order.ReceiverName,
                    Address = order.ShippingAddress,
                    Province = order.ShippingProvince,
                    District = order.ShippingDistrict,
                    Ward = order.ShippingWard,
                    Hamlet = "Khác",
                    IsFreeship = "0",
                    PickMoney = order.PaymentMethod == "COD" ? (int)order.TotalAmount : 0,
                    Note = $"Đơn hàng TeviaFarm #{order.OrderId}",
                    Value = (int)order.TotalAmount,
                    Transport = "road"
                },
                Products = order.OrderDetails.Select(d => new GhtkProductItem
                {
                    Name = d.Product?.ProductName ?? $"SP-{d.ProductId}",
                    Weight = (int)(((d.Product?.WeightKg ?? 0.5) * 1000)),
                    Quantity = d.Quantity,
                    ProductCode = d.ProductId.ToString()
                }).ToList()
            };

            var json = JsonSerializer.Serialize(requestModel);

            using var request = new HttpRequestMessage(HttpMethod.Post, "services/shipment/order");
            request.Headers.Add("Token", token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("GHTK Create Order Error: " + responseText);
                return null;
            }

            var result = JsonSerializer.Deserialize<GhtkCreateOrderResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
    }

    public class GhtkFeeResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("fee")]
        public GhtkFeeData? Fee { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class GhtkFeeData
    {
        [JsonPropertyName("fee")]
        public decimal FeeValue { get; set; }
    }

    public class GhtkCreateOrderRequest
    {
        [JsonPropertyName("products")]
        public List<GhtkProductItem> Products { get; set; } = new();

        [JsonPropertyName("order")]
        public GhtkOrderInfo Order { get; set; } = new();
    }

    public class GhtkProductItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public int Weight { get; set; } // gram

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("product_code")]
        public string ProductCode { get; set; } = string.Empty;
    }

    public class GhtkOrderInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("pick_name")]
        public string PickName { get; set; } = string.Empty;

        [JsonPropertyName("pick_address")]
        public string PickAddress { get; set; } = string.Empty;

        [JsonPropertyName("pick_province")]
        public string PickProvince { get; set; } = string.Empty;

        [JsonPropertyName("pick_district")]
        public string PickDistrict { get; set; } = string.Empty;

        [JsonPropertyName("pick_tel")]
        public string PickTel { get; set; } = string.Empty;

        [JsonPropertyName("tel")]
        public string Tel { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("province")]
        public string Province { get; set; } = string.Empty;

        [JsonPropertyName("district")]
        public string District { get; set; } = string.Empty;

        [JsonPropertyName("ward")]
        public string Ward { get; set; } = string.Empty;

        [JsonPropertyName("hamlet")]
        public string Hamlet { get; set; } = "Khác";

        [JsonPropertyName("is_freeship")]
        public string IsFreeship { get; set; } = "0";

        [JsonPropertyName("pick_money")]
        public int PickMoney { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public int Value { get; set; }

        [JsonPropertyName("transport")]
        public string Transport { get; set; } = "road";
    }

    public class GhtkCreateOrderResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("order")]
        public GhtkCreatedOrderData? Order { get; set; }
    }

    public class GhtkCreatedOrderData
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("status_id")]
        public int? StatusId { get; set; }

        [JsonPropertyName("partner_id")]
        public string? PartnerId { get; set; }
    }
}