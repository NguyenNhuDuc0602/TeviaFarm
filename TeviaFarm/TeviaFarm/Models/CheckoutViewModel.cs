using System.ComponentModel.DataAnnotations;

namespace TeviaFarm.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Họ tên người nhận không được để trống.")]
        [StringLength(100)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại người nhận không được để trống.")]
        [StringLength(20)]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống.")]
        [StringLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phường/Xã không được để trống.")]
        [StringLength(100)]
        public string ShippingWard { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quận/Huyện không được để trống.")]
        [StringLength(100)]
        public string ShippingDistrict { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống.")]
        [StringLength(100)]
        public string ShippingProvince { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = "COD";

        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
    }
}