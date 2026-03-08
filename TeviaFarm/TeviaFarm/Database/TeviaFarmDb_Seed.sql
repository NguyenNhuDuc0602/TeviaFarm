USE TeviaFarmDb;
GO

-- Seed Users (tài khoản mẫu)
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (UserId, Username, [Password], Email, [Role], CreatedDate) VALUES
(1, 'admin', 'admin123', 'admin@teviafarm.local', 'Admin', SYSUTCDATETIME()),
(2, 'khachhang1', '123456', 'khachhang1@teviafarm.local', 'Customer', SYSUTCDATETIME()),
(3, 'nongdan1', '123456', 'nongdan1@teviafarm.local', 'Farmer', SYSUTCDATETIME());
SET IDENTITY_INSERT Users OFF;

-- Seed Products (sản phẩm thịt heo trà xanh)
SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductId, ProductName, [Description], Price, Stock, ImageUrl, CategoryId) VALUES
(1, N'Ba rọi heo trà xanh', N'Thịt ba rọi từ heo nuôi bằng khẩu phần có bột trà xanh, thơm ngon và an toàn.', 150000, 50, N'~/images/ba-roi-heo-tra-xanh.jpg', 1),
(2, N'Thịt vai heo trà xanh', N'Thịt vai mềm, giàu dinh dưỡng, heo được cho ăn công thức trà xanh.', 130000, 40, N'~/images/thit-vai-heo-tra-xanh.jpg', 1),
(3, N'Xúc xích heo trà xanh', N'Xúc xích làm thủ công từ thịt heo nuôi bằng trà xanh, phù hợp cho gia đình.', 90000, 100, N'~/images/xuc-xich-heo-tra-xanh.jpg', 2),
(4, N'Thức ăn trộn bột trà xanh', N'Thức ăn hỗn hợp có bổ sung bột trà xanh, giúp tăng sức đề kháng cho đàn heo.', 500000, 20, N'~/images/thuc-an-tra-xanh.jpg', 3),
(5, N'Bột trà xanh nguyên chất', N'Bột trà xanh dùng để phối trộn vào khẩu phần ăn của heo, giúp cải thiện sức khỏe và chất lượng thịt.', 120000, 100, N'~/images/bot-tra-xanh.jpg', 4),
(6, N'Cám gạo cho heo', N'Cám gạo dùng kết hợp với bột trà xanh để tạo khẩu phần ăn cân đối cho đàn heo.', 80000, 100, N'~/images/cam-gao.jpg', 4),
(7, N'Lá trà xanh phơi khô', N'Lá trà xanh được phơi và sấy khô, dùng để nghiền làm bột hoặc pha trộn trực tiếp vào thức ăn cho heo.', 60000, 80, N'~/images/la-tra-xanh.jpg', 4);
SET IDENTITY_INSERT Products OFF;

-- Seed Courses (khóa học)
SET IDENTITY_INSERT Courses ON;
INSERT INTO Courses (CourseId, Title, [Description], Price) VALUES
(1, N'Giới thiệu mô hình nuôi heo trà xanh', N'Tổng quan về mô hình nuôi heo có bổ sung bột trà xanh tại Tevia Farm.', 0),
(2, N'Kỹ thuật phối trộn thức ăn nâng cao', N'Tối ưu khẩu phần và tốc độ tăng trưởng nhờ công thức trà xanh.', 300000);
SET IDENTITY_INSERT Courses OFF;

-- Seed Lessons (bài học)
SET IDENTITY_INSERT Lessons ON;
INSERT INTO Lessons (LessonId, CourseId, Title, VideoUrl) VALUES
(1, 1, N'Tổng quan mô hình trang trại', 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4'),
(2, 1, N'Nguyên tắc an toàn sinh học', 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4'),
(3, 2, N'Cách phối trộn thức ăn có bột trà xanh', 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerJoyrides.mp4'),
(4, 2, N'Các giai đoạn nuôi và khẩu phần', 'https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4');
SET IDENTITY_INSERT Lessons OFF;

-- Seed Posts (bài viết cộng đồng)
SET IDENTITY_INSERT Posts ON;
INSERT INTO Posts (PostId, UserId, Title, Content, ImageUrl, CreatedDate, IsApproved) VALUES
(1, 3, N'Lứa heo trà xanh đầu tiên', N'Lứa heo đầu tiên mình áp dụng khẩu phần có bột trà xanh tăng trọng khá đều, sức khỏe ổn định.', NULL, SYSUTCDATETIME(), 1),
(2, 3, N'Hỏi về tỷ lệ phối trộn', N'Mọi người đang dùng tỷ lệ bột trà xanh như thế nào cho heo hậu bị và heo xuất chuồng?', NULL, SYSUTCDATETIME(), 1);
SET IDENTITY_INSERT Posts OFF;

-- Seed Orders (đơn hàng mẫu)
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderId, UserId, TotalAmount, [Status], ShippingAddress, OrderDate) VALUES
(1, 2, 430000, N'Hoàn tất', N'123 Đường Trà Xanh, Hà Nội', SYSUTCDATETIME());
SET IDENTITY_INSERT Orders OFF;

-- Seed OrderDetails
SET IDENTITY_INSERT OrderDetails ON;
INSERT INTO OrderDetails (OrderDetailId, OrderId, ProductId, Quantity, Price) VALUES
(1, 1, 1, 1, 150000),
(2, 1, 2, 1, 130000),
(3, 1, 3, 1, 90000),
(4, 1, 4, 1, 60000);
SET IDENTITY_INSERT OrderDetails OFF;

-- Seed Cart and CartItems (optional demo)
SET IDENTITY_INSERT Carts ON;
INSERT INTO Carts (CartId, UserId) VALUES
(1, 2);
SET IDENTITY_INSERT Carts OFF;

SET IDENTITY_INSERT CartItems ON;
INSERT INTO CartItems (CartItemId, CartId, ProductId, Quantity) VALUES
(1, 1, 1, 2),
(2, 1, 3, 3);
SET IDENTITY_INSERT CartItems OFF;

