using System.ComponentModel.DataAnnotations;

namespace TeviaFarm.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Customer"; // Guest, Customer, Farmer, Admin

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
        public ICollection<CourseOrder> CourseOrders { get; set; } = new List<CourseOrder>();
    }

    public class Product
    {
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public int? CategoryId { get; set; }
    }

    public class Cart
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public Cart? Cart { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        [Required, StringLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // COD, ChuyenKhoan

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;
    }

    public class Course
    {
        public int CourseId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
        public ICollection<CourseOrderDetail> CourseOrderDetails { get; set; } = new List<CourseOrderDetail>();
    }

    public class Lesson
    {
        public int LessonId { get; set; }
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(255)]
        public string? VideoUrl { get; set; }
    }

    public class UserCourse
    {
        public int UserCourseId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        public DateTime EnrolledDate { get; set; } = DateTime.UtcNow;
    }

    public class CourseOrder
    {
        public int CourseOrderId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<CourseOrderDetail> CourseOrderDetails { get; set; } = new List<CourseOrderDetail>();
    }

    public class CourseOrderDetail
    {
        public int CourseOrderDetailId { get; set; }

        public int CourseOrderId { get; set; }
        public CourseOrder? CourseOrder { get; set; }

        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}