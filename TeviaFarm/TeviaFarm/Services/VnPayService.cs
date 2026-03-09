using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace TeviaFarm.Services
{
    public class VnPayService
    {
        public string CreatePaymentUrl(
            string baseUrl,
            string hashSecret,
            SortedDictionary<string, string> data)
        {
            var query = new StringBuilder();

            foreach (var kv in data)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                {
                    query.Append(WebUtility.UrlEncode(kv.Key));
                    query.Append('=');
                    query.Append(WebUtility.UrlEncode(kv.Value));
                    query.Append('&');
                }
            }

            if (query.Length > 0)
                query.Length--;

            var signData = query.ToString();
            var secureHash = HmacSHA512(hashSecret, signData);

            return $"{baseUrl}?{signData}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(
            IQueryCollection query,
            string hashSecret,
            out SortedDictionary<string, string> data,
            out string inputHash)
        {
            data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            inputHash = query["vnp_SecureHash"].ToString();

            foreach (var item in query)
            {
                if (item.Key.StartsWith("vnp_") &&
                    item.Key != "vnp_SecureHash" &&
                    item.Key != "vnp_SecureHashType")
                {
                    data[item.Key] = item.Value.ToString();
                }
            }

            var raw = new StringBuilder();
            foreach (var kv in data)
            {
                raw.Append(WebUtility.UrlEncode(kv.Key));
                raw.Append('=');
                raw.Append(WebUtility.UrlEncode(kv.Value));
                raw.Append('&');
            }

            if (raw.Length > 0)
                raw.Length--;

            var myHash = HmacSHA512(hashSecret, raw.ToString());
            return myHash.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            using var hmac = new HMACSHA512(keyBytes);
            var hashValue = hmac.ComputeHash(inputBytes);

            foreach (var b in hashValue)
                hash.Append(b.ToString("x2"));

            return hash.ToString();
        }
    }
}