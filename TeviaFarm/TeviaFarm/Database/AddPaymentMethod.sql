-- Thêm cột PaymentMethod vào bảng Orders (cho luồng thanh toán mới)
USE TeviaFarmDb;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'PaymentMethod')
BEGIN
    ALTER TABLE Orders ADD PaymentMethod NVARCHAR(50) NULL;
END
GO
