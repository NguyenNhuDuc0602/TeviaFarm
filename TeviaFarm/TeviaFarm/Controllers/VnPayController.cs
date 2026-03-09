using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeviaFarm.Data;
using TeviaFarm.Services;

namespace TeviaFarm.Controllers
{
    public class VnPayController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly VnPayService _vnPayService;
        private readonly ILogger<VnPayController> _logger;

        public VnPayController(
            AppDbContext context,
            IConfiguration config,
            VnPayService vnPayService,
            ILogger<VnPayController> logger)
        {
            _context = context;
            _config = config;
            _vnPayService = vnPayService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Return()
        {
            try
            {
                var secret = _config["VnPay:HashSecret"];
                if (string.IsNullOrWhiteSpace(secret))
                {
                    TempData["ToastMessage"] = "Thiếu cấu hình HashSecret của VNPAY.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Index", "Home");
                }

                var valid = _vnPayService.ValidateSignature(Request.Query, secret, out var data, out _);

                if (!valid)
                {
                    TempData["ToastMessage"] = "Chữ ký VNPAY không hợp lệ.";
                    TempData["ToastType"] = "danger";
                    return RedirectToAction("Index", "Home");
                }

                var responseCode = data.ContainsKey("vnp_ResponseCode")
                    ? data["vnp_ResponseCode"]
                    : "";

                var transactionStatus = data.ContainsKey("vnp_TransactionStatus")
                    ? data["vnp_TransactionStatus"]
                    : "";

                if (responseCode == "00" && transactionStatus == "00")
                {
                    TempData["ToastMessage"] = "Thanh toán thành công. Hệ thống đang xác nhận giao dịch.";
                    TempData["ToastType"] = "success";
                }
                else
                {
                    TempData["ToastMessage"] = $"Thanh toán chưa thành công. Mã lỗi: {responseCode}";
                    TempData["ToastType"] = "warning";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tại VnPay Return");
                return Content("Lỗi Return VNPAY: " + ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Ipn()
        {
            try
            {
                var secret = _config["VnPay:HashSecret"];
                if (string.IsNullOrWhiteSpace(secret))
                {
                    return Json(new { RspCode = "99", Message = "Missing config" });
                }

                var valid = _vnPayService.ValidateSignature(Request.Query, secret, out var data, out _);

                if (!valid)
                    return Json(new { RspCode = "97", Message = "Invalid signature" });

                var txnRef = data.ContainsKey("vnp_TxnRef") ? data["vnp_TxnRef"] : "";
                var responseCode = data.ContainsKey("vnp_ResponseCode") ? data["vnp_ResponseCode"] : "";
                var transactionStatus = data.ContainsKey("vnp_TransactionStatus") ? data["vnp_TransactionStatus"] : "";
                var transactionNo = data.ContainsKey("vnp_TransactionNo") ? data["vnp_TransactionNo"] : "";
                var amountRaw = data.ContainsKey("vnp_Amount") ? data["vnp_Amount"] : "0";
                var payDateRaw = data.ContainsKey("vnp_PayDate") ? data["vnp_PayDate"] : "";

                if (!long.TryParse(amountRaw, out var amountValue))
                {
                    return Json(new { RspCode = "99", Message = "Invalid amount format" });
                }

                var amount = amountValue / 100;

                DateTime? payDate = null;
                if (!string.IsNullOrWhiteSpace(payDateRaw) && payDateRaw.Length == 14)
                {
                    if (DateTime.TryParseExact(
                        payDateRaw,
                        "yyyyMMddHHmmss",
                        null,
                        System.Globalization.DateTimeStyles.None,
                        out var parsed))
                    {
                        payDate = parsed;
                    }
                }

                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.VnpTxnRef == txnRef);

                if (order != null)
                {
                    if ((long)order.TotalAmount != amount)
                        return Json(new { RspCode = "04", Message = "Invalid amount" });

                    if (order.Status == "Cancelled")
                        return Json(new { RspCode = "02", Message = "Order already cancelled" });

                    if (order.PaymentStatus == "Paid")
                        return Json(new { RspCode = "02", Message = "Order already confirmed" });

                    if (responseCode == "00" && transactionStatus == "00")
                    {
                        order.PaymentStatus = "Paid";
                        order.PaymentResponseCode = responseCode;
                        order.VnpTransactionNo = transactionNo;
                        order.PaymentDate = payDate;
                        order.Status = "Pending";
                    }
                    else
                    {
                        order.PaymentStatus = "Failed";
                        order.PaymentResponseCode = responseCode;
                        order.Status = "Cancelled";
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { RspCode = "00", Message = "Confirm Success" });
                }

                var courseOrder = await _context.CourseOrders
                    .Include(x => x.CourseOrderDetails)
                    .FirstOrDefaultAsync(x => x.VnpTxnRef == txnRef);

                if (courseOrder != null)
                {
                    if ((long)courseOrder.TotalAmount != amount)
                        return Json(new { RspCode = "04", Message = "Invalid amount" });

                    if (courseOrder.Status == "Cancelled")
                        return Json(new { RspCode = "02", Message = "Order already cancelled" });

                    if (courseOrder.PaymentStatus == "Paid")
                        return Json(new { RspCode = "02", Message = "Order already confirmed" });

                    if (responseCode == "00" && transactionStatus == "00")
                    {
                        courseOrder.PaymentStatus = "Paid";
                        courseOrder.PaymentResponseCode = responseCode;
                        courseOrder.VnpTransactionNo = transactionNo;
                        courseOrder.PaymentDate = payDate;
                        courseOrder.Status = "Paid";

                        foreach (var detail in courseOrder.CourseOrderDetails)
                        {
                            var alreadyOwned = await _context.UserCourses
                                .AnyAsync(x => x.UserId == courseOrder.UserId && x.CourseId == detail.CourseId);

                            if (!alreadyOwned)
                            {
                                _context.UserCourses.Add(new TeviaFarm.Models.UserCourse
                                {
                                    UserId = courseOrder.UserId,
                                    CourseId = detail.CourseId,
                                    EnrolledDate = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    else
                    {
                        courseOrder.PaymentStatus = "Failed";
                        courseOrder.PaymentResponseCode = responseCode;
                        courseOrder.Status = "Cancelled";
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { RspCode = "00", Message = "Confirm Success" });
                }

                return Json(new { RspCode = "01", Message = "Order not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tại VnPay IPN");
                return Json(new { RspCode = "99", Message = ex.Message });
            }
        }
    }
}