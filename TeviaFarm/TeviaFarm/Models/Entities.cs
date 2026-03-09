using System.ComponentModel.DataAnnotations;

namespace TeviaFarm.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vai trò không hợp lệ.")]
        public string Role { get; set; } = "Customer"; // Guest, Customer, Farmer, Admin

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
        public ICollection<CourseOrder> CourseOrders { get; set; } = new List<CourseOrder>();
    }

    public class Product
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên sản phẩm phải từ 2 đến 100 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải lớn hơn hoặc bằng 0.")]
        public int Stock { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 255 ký tự.")]
        [RegularExpression(@"^(~\/|\/|https?:\/\/).*$", ErrorMessage = "Đường dẫn hình ảnh không hợp lệ. Hãy dùng dạng `~/...`, `/...` hoặc `http(s)://...`.")]
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

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn hoặc bằng 0.")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Trạng thái đơn hàng không hợp lệ.")]
        public string Status { get; set; } = "Pending";

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống.")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ giao hàng phải từ 5 đến 255 ký tự.")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
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

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Nội dung phải từ 10 đến 4000 ký tự.")]
        public string Content { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 255 ký tự.")]
        [RegularExpression(@"^(~\/|\/|https?:\/\/).*$", ErrorMessage = "Đường dẫn hình ảnh không hợp lệ. Hãy dùng dạng `~/...`, `/...` hoặc `http(s)://...`.")]
        public string? ImageUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;
    }

    public class Course
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Tiêu đề khóa học không được để trống.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Tiêu đề khóa học phải từ 5 đến 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000, ErrorMessage = "Mô tả khóa học không được vượt quá 4000 ký tự.")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá khóa học phải lớn hơn hoặc bằng 0.")]
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

        [Required(ErrorMessage = "Tiêu đề bài học không được để trống.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Tiêu đề bài học phải từ 3 đến 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Link video không được vượt quá 255 ký tự.")]
        [RegularExpression(@"^(https?:\/\/.*|\/.*|~\/.*|.*\.mp4)$", ErrorMessage = "Link video không hợp lệ. Hãy nhập URL hoặc đường dẫn `.mp4`.")]
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

        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn hoặc bằng 0.")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Trạng thái đơn không hợp lệ.")]
        [StringLength(50, ErrorMessage = "Trạng thái đơn không hợp lệ.")]
        public string Status { get; set; } = "Pending";

        [StringLength(50, ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
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

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }
    }
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu chấm và dấu gạch dưới.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(254, ErrorMessage = "Email không được vượt quá 254 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ thường, 1 chữ hoa và 1 chữ số.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}