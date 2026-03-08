using Microsoft.AspNetCore.Mvc;

namespace TeviaFarm.Controllers
{
    public class ToolController : Controller
    {
        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }

        [HttpGet]
        public IActionResult FeedCalculator()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public IActionResult FeedCalculator(int numberOfPigs, decimal averageWeight, string growthStage)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }
            // Simple demo formula – adjust as needed
            decimal baseFeedPerKg = growthStage switch
            {
                "Starter" => 0.05m,
                "Grower" => 0.04m,
                "Finisher" => 0.03m,
                _ => 0.04m
            };

            var totalFeed = numberOfPigs * averageWeight * baseFeedPerKg;
            var greenTeaPowder = totalFeed * 0.03m; // 3% green tea

            ViewBag.NumberOfPigs = numberOfPigs;
            ViewBag.AverageWeight = averageWeight;
            ViewBag.GrowthStage = growthStage;
            ViewBag.TotalFeed = totalFeed;
            ViewBag.GreenTeaPowder = greenTeaPowder;
            ViewBag.MixingRatio = "3% green tea powder, 97% feed";

            return View();
        }
    }
}

