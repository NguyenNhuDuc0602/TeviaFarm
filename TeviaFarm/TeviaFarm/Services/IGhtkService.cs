using TeviaFarm.Models;

namespace TeviaFarm.Services
{
    public interface IGhtkService
    {
        Task<decimal?> CalculateShippingFeeAsync(string toProvince, string toDistrict, double weightKg, decimal value);
        Task<GhtkCreateOrderResponse?> CreateOrderAsync(Order order);
    }
}