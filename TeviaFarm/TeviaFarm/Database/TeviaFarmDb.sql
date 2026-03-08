CREATE DATABASE TeviaFarmDb;
GO

USE TeviaFarmDb;
GO

-- Users
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    [Password] NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL DEFAULT 'Customer',
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Products
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    Stock INT NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(255) NULL,
    CategoryId INT NULL
);

-- Carts
CREATE TABLE Carts (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL
        FOREIGN KEY REFERENCES Users(UserId)
);

-- CartItems
CREATE TABLE CartItems (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL
        FOREIGN KEY REFERENCES Carts(CartId),
    ProductId INT NOT NULL
        FOREIGN KEY REFERENCES Products(ProductId),
    Quantity INT NOT NULL
);

-- Orders
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL
        FOREIGN KEY REFERENCES Users(UserId),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    ShippingAddress NVARCHAR(255) NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- OrderDetails
CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL
        FOREIGN KEY REFERENCES Orders(OrderId),
    ProductId INT NOT NULL
        FOREIGN KEY REFERENCES Products(ProductId),
    Quantity INT NOT NULL,
    Price DECIMAL(18,2) NOT NULL
);

-- Posts (Community)
CREATE TABLE Posts (
    PostId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL
        FOREIGN KEY REFERENCES Users(UserId),
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsApproved BIT NOT NULL DEFAULT 0
);

-- Courses
CREATE TABLE Courses (
    CourseId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL DEFAULT 0
);

-- Lessons
CREATE TABLE Lessons (
    LessonId INT IDENTITY(1,1) PRIMARY KEY,
    CourseId INT NOT NULL
        FOREIGN KEY REFERENCES Courses(CourseId),
    Title NVARCHAR(200) NOT NULL,
    VideoUrl NVARCHAR(255) NULL
);

