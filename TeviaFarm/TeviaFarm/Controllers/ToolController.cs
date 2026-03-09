using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TeviaFarm.Controllers
{
    [Authorize]
    public class ToolController : Controller
    {
        [HttpGet]
        public IActionResult FeedCalculator()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FeedCalculator(int numberOfPigs, decimal averageWeight, string growthStage)
        {
            growthStage = growthStage?.Trim() ?? "";

            var allowedStages = new[] { "Starter", "Grower", "Finisher" };

            if (numberOfPigs <= 0)
            {
                ModelState.AddModelError(string.Empty, "Số lượng heo phải lớn hơn 0.");
            }

            if (averageWeight <= 0)
            {
                ModelState.AddModelError(string.Empty, "Khối lượng trung bình phải lớn hơn 0.");
            }

            if (string.IsNullOrWhiteSpace(growthStage) || !allowedStages.Contains(growthStage))
            {
                ModelState.AddModelError(string.Empty, "Giai đoạn phát triển không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            decimal baseFeedPerKg = growthStage switch
            {
                "Starter" => 0.05m,
                "Grower" => 0.04m,
                "Finisher" => 0.03m,
                _ => 0.04m
            };

            string growthStageDisplay = growthStage switch
            {
                "Starter" => "Heo con",
                "Grower" => "Heo đang lớn",
                "Finisher" => "Heo xuất chuồng",
                _ => growthStage
            };

            var totalFeed = numberOfPigs * averageWeight * baseFeedPerKg;
            var greenTeaPowder = totalFeed * 0.03m;

            ViewBag.NumberOfPigs = numberOfPigs;
            ViewBag.AverageWeight = averageWeight;
            ViewBag.GrowthStage = growthStageDisplay;
            ViewBag.TotalFeed = totalFeed;
            ViewBag.GreenTeaPowder = greenTeaPowder;
            ViewBag.MixingRatio = "3% bột trà xanh, 97% cám";

            return View();
        }
    }
}