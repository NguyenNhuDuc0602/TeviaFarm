using System.Text.Json.Serialization;

namespace TeviaFarm.Models
{
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
        public int Weight { get; set; }  // gram

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