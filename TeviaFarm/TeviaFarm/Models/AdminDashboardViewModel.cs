using System.Collections.Generic;

namespace TeviaFarm.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal ProductRevenue { get; set; }

        public int TotalCourseOrders { get; set; }
        public decimal CourseRevenue { get; set; }

        public int TotalUsers { get; set; }
        public int PendingPosts { get; set; }

        public List<Order> LatestOrders { get; set; } = new();
        public List<Post> LatestPosts { get; set; } = new();

        public decimal TotalRevenue => ProductRevenue + CourseRevenue;
    }
}