using Microsoft.AspNetCore.Mvc;

namespace TeviaFarm.Controllers
{
    public class ToolController : Controller
    {
        [HttpGet]
        public IActionResult FeedCalculator()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FeedCalculator(int pigCount, double averageWeight, string stage)
        {
            if (pigCount <= 0 || averageWeight <= 0 || string.IsNullOrWhiteSpace(stage))
            {
                ViewBag.Result = "Vui lòng nhập đầy đủ và hợp lệ tất cả thông tin.";
                return View();
            }

            double feedRate;
            double greenTeaRate;

            switch (stage)
            {
                case "piglet":
                    feedRate = 0.05;      // 5% khối lượng cơ thể
                    greenTeaRate = 0.03;  // 3% trong khẩu phần
                    break;
                case "growing":
                    feedRate = 0.04;
                    greenTeaRate = 0.05;
                    break;
                case "finishing":
                    feedRate = 0.035;
                    greenTeaRate = 0.04;
                    break;
                default:
                    ViewBag.Result = "Giai đoạn phát triển không hợp lệ.";
                    return View();
            }

            double totalWeight = pigCount * averageWeight;
            double totalFeed = totalWeight * feedRate;
            double greenTeaAmount = totalFeed * greenTeaRate;
            double normalFeedAmount = totalFeed - greenTeaAmount;

            ViewBag.TotalFeed = totalFeed.ToString("0.##");
            ViewBag.GreenTeaAmount = greenTeaAmount.ToString("0.##");
            ViewBag.NormalFeedAmount = normalFeedAmount.ToString("0.##");
            ViewBag.Result = "ok";

            return View();
        }
    }
}