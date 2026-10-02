-- ========================================================
-- 1. TẠO DATABASE
-- ========================================================
CREATE DATABASE db_trasua;
GO

USE db_trasua;
GO

-- ========================================================
-- 2. TẠO BẢNG
-- ========================================================

CREATE TABLE [categories] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [name] NVARCHAR(255) NOT NULL,
  [is_active] BIT NOT NULL DEFAULT 1,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [ingredients] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [name] NVARCHAR(255) NOT NULL,
  [unit] NVARCHAR(20) NOT NULL,
  [stock_quantity] FLOAT NOT NULL DEFAULT 0,
  [min_stock] FLOAT NOT NULL DEFAULT 0,
  [consumption_role] NVARCHAR(50) NOT NULL DEFAULT 'normal',
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [users] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [name] NVARCHAR(255) NOT NULL,
  [email] NVARCHAR(255) NOT NULL UNIQUE,
  [phone] NVARCHAR(20) DEFAULT NULL,
  [role] NVARCHAR(50) NOT NULL DEFAULT 'customer',
  [status] BIT NOT NULL DEFAULT 1,
  [points] INT NOT NULL DEFAULT 0,
  [tier] NVARCHAR(255) NOT NULL DEFAULT N'Đồng',
  [email_verified_at] DATETIME NULL DEFAULT NULL,
  [password] NVARCHAR(255) NOT NULL,
  [remember_token] NVARCHAR(100) DEFAULT NULL,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [vouchers] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [code] NVARCHAR(255) NOT NULL UNIQUE,
  [name] NVARCHAR(255) NOT NULL,
  [discount_type] NVARCHAR(50) NOT NULL,
  [discount_value] INT NOT NULL,
  [max_discount] INT DEFAULT NULL,
  [min_order_value] INT NOT NULL DEFAULT 0,
  [required_tier] NVARCHAR(255) DEFAULT NULL,
  [usage_limit] INT DEFAULT NULL,
  [used_count] INT NOT NULL DEFAULT 0,
  [expires_at] DATETIME DEFAULT NULL,
  [is_active] BIT NOT NULL DEFAULT 1,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [toppings] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [name] NVARCHAR(255) NOT NULL,
  [price] DECIMAL(12,0) NOT NULL,
  [ingredient_id] BIGINT DEFAULT NULL,
  [amount_per_serving] FLOAT DEFAULT NULL,
  [is_active] BIT NOT NULL DEFAULT 1,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [products] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [category_id] BIGINT NOT NULL,
  [name] NVARCHAR(255) NOT NULL,
  [image] NVARCHAR(255) DEFAULT NULL,
  [description] NVARCHAR(MAX) DEFAULT NULL,
  [is_active] BIT NOT NULL DEFAULT 1,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [product_variants] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [product_id] BIGINT NOT NULL,
  [size] NVARCHAR(10) NOT NULL,
  [price] DECIMAL(12,0) NOT NULL,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL,
  CONSTRAINT [UC_product_variants] UNIQUE ([product_id], [size])
);

CREATE TABLE [recipe_details] (
  [variant_id] BIGINT NOT NULL,
  [ingredient_id] BIGINT NOT NULL,
  [amount] FLOAT NOT NULL,
  PRIMARY KEY ([variant_id], [ingredient_id])
);

CREATE TABLE [orders] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [customer_id] BIGINT DEFAULT NULL,
  [staff_id] BIGINT DEFAULT NULL,
  [subtotal] DECIMAL(12,0) NOT NULL,
  [shipping_fee] DECIMAL(12,0) NOT NULL DEFAULT 0,
  [discount_amount] DECIMAL(12,0) NOT NULL DEFAULT 0,
  [final_total] DECIMAL(12,0) NOT NULL,
  [payment_method] NVARCHAR(50) NOT NULL DEFAULT 'cash',
  [payment_status] NVARCHAR(50) NOT NULL DEFAULT 'unpaid',
  [order_status] NVARCHAR(50) NOT NULL DEFAULT 'pending',
  [order_type] NVARCHAR(50) NOT NULL,
  [shipping_name] NVARCHAR(255) DEFAULT NULL,
  [shipping_phone] NVARCHAR(20) DEFAULT NULL,
  [shipping_address] NVARCHAR(MAX) DEFAULT NULL,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [order_items] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [order_id] BIGINT NOT NULL,
  [variant_id] BIGINT NOT NULL,
  [quantity] INT NOT NULL,
  [unit_price] DECIMAL(12,0) NOT NULL,
  [sugar_level] NVARCHAR(10) NOT NULL DEFAULT '100',
  [ice_option] NVARCHAR(50) NOT NULL DEFAULT 'normal',
  [note] NVARCHAR(255) DEFAULT NULL,
  [created_at] DATETIME NULL DEFAULT NULL,
  [updated_at] DATETIME NULL DEFAULT NULL
);

CREATE TABLE [order_item_toppings] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [order_item_id] BIGINT NOT NULL,
  [topping_id] BIGINT DEFAULT NULL,
  [topping_name] NVARCHAR(255) NOT NULL,
  [price] DECIMAL(12,0) NOT NULL
);

CREATE TABLE [inventory_transactions] (
  [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [ingredient_id] BIGINT NOT NULL,
  [order_id] BIGINT DEFAULT NULL,
  [quantity_changed] FLOAT NOT NULL,
  [total_cost] DECIMAL(12,0) DEFAULT NULL,
  [unit_cost] DECIMAL(12,4) DEFAULT NULL,
  [transaction_type] NVARCHAR(50) NOT NULL,
  [note] NVARCHAR(255) DEFAULT NULL,
  [created_at] DATETIME NULL DEFAULT GETDATE()
);
GO

-- ========================================================
-- 3. TẠO KHÓA NGOẠI (FOREIGN KEYS)
-- ========================================================

ALTER TABLE [toppings] ADD CONSTRAINT [FK_toppings_ingredient] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([id]) ON DELETE SET NULL;
ALTER TABLE [products] ADD CONSTRAINT [FK_products_category] FOREIGN KEY ([category_id]) REFERENCES [categories] ([id]);
ALTER TABLE [product_variants] ADD CONSTRAINT [FK_product_variants_product] FOREIGN KEY ([product_id]) REFERENCES [products] ([id]) ON DELETE CASCADE;
ALTER TABLE [recipe_details] ADD CONSTRAINT [FK_recipe_details_ingredient] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([id]) ON DELETE CASCADE;
ALTER TABLE [recipe_details] ADD CONSTRAINT [FK_recipe_details_variant] FOREIGN KEY ([variant_id]) REFERENCES [product_variants] ([id]) ON DELETE CASCADE;
ALTER TABLE [orders] ADD CONSTRAINT [FK_orders_customer] FOREIGN KEY ([customer_id]) REFERENCES [users] ([id]) ON DELETE SET NULL;
ALTER TABLE [orders] ADD CONSTRAINT [FK_orders_staff] FOREIGN KEY ([staff_id]) REFERENCES [users] ([id]) ON DELETE SET NULL;
ALTER TABLE [order_items] ADD CONSTRAINT [FK_order_items_order] FOREIGN KEY ([order_id]) REFERENCES [orders] ([id]) ON DELETE CASCADE;
ALTER TABLE [order_items] ADD CONSTRAINT [FK_order_items_variant] FOREIGN KEY ([variant_id]) REFERENCES [product_variants] ([id]);
ALTER TABLE [order_item_toppings] ADD CONSTRAINT [FK_order_item_toppings_order_item] FOREIGN KEY ([order_item_id]) REFERENCES [order_items] ([id]) ON DELETE CASCADE;
ALTER TABLE [order_item_toppings] ADD CONSTRAINT [FK_order_item_toppings_topping] FOREIGN KEY ([topping_id]) REFERENCES [toppings] ([id]) ON DELETE SET NULL;
ALTER TABLE [inventory_transactions] ADD CONSTRAINT [FK_inv_trans_ingredient] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([id]) ON DELETE CASCADE;
ALTER TABLE [inventory_transactions] ADD CONSTRAINT [FK_inv_trans_order] FOREIGN KEY ([order_id]) REFERENCES [orders] ([id]) ON DELETE SET NULL;
GO

-- ========================================================
-- 4. THÊM DỮ LIỆU
-- ========================================================

-- --- CATEGORIES ---
SET IDENTITY_INSERT [categories] ON;
INSERT INTO [categories] ([id], [name], [is_active], [created_at], [updated_at]) VALUES
(1, N'Trà Sữa', 1, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(2, N'Trà Trái Cây', 1, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(3, N'Cà Phê', 1, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(4, N'Đá Xay', 1, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(5, N'Matcha', 1, '2026-09-23 18:40:35', '2026-09-23 18:40:35');
SET IDENTITY_INSERT [categories] OFF;

-- --- INGREDIENTS ---
SET IDENTITY_INSERT [ingredients] ON;
INSERT INTO [ingredients] ([id], [name], [unit], [stock_quantity], [min_stock], [consumption_role], [created_at], [updated_at]) VALUES
(1, N'Trà đen', 'g', 4778.800000000001, 500, 'normal', '2026-09-16 20:32:52', '2026-09-25 16:20:32'),
(2, N'Trà xanh', 'g', 3946.6, 500, 'normal', '2026-09-16 20:32:52', '2026-09-25 15:12:50'),
(3, N'Bột matcha', 'g', 1868.6, 300, 'normal', '2026-09-16 20:32:52', '2026-09-25 16:20:32'),
(4, N'Sữa tươi', 'ml', 4299, 1000, 'normal', '2026-09-16 20:32:52', '2026-09-25 16:20:32'),
(5, N'Bột kem béo', 'g', 5628, 800, 'normal', '2026-09-16 20:32:52', '2026-09-25 16:20:32'),
(6, N'Trân châu đen', 'g', 7305, 1000, 'normal', '2026-09-16 20:32:52', '2026-09-25 12:00:36'),
(7, N'Đường', 'g', 8700.75, 1000, 'sugar', '2026-09-16 20:32:52', '2026-09-25 16:20:32'),
(8, N'Đá viên', 'g', 37424, 5000, 'ice', '2026-09-16 20:32:52', '2026-09-25 16:20:32'),
(9, N'Cà phê', 'g', 2878, 400, 'normal', '2026-09-16 20:32:52', '2026-09-25 15:12:50'),
(10, N'Bột cacao', 'g', 0, 500, 'normal', '2026-09-16 20:32:52', '2026-09-24 18:29:55'),
(11, N'Dâu tây xay', 'g', 2008, 500, 'normal', '2026-09-16 20:32:52', '2026-09-25 13:30:31'),
(12, N'sữa đặc', 'ml', 9970, 0, 'normal', '2026-09-23 18:42:57', '2026-09-25 13:41:20'),
(13, N'kem machiato', 'g', 985, 0, 'normal', '2026-09-23 18:43:43', '2026-09-25 13:41:20'),
(14, N'Trân châu trắng', 'g', 9860, 0, 'normal', '2026-09-25 10:39:52', '2026-09-25 13:30:31'),
(15, N'Trà đen Phúc Long', 'kg', 10, 0, 'normal', '2026-09-25 12:00:36', '2026-09-25 12:00:36'),
(16, N'Sữa đặc Ngôi Sao Phương Nam', N'hộp', 24, 0, 'normal', '2026-09-25 12:00:36', '2026-09-25 12:00:36'),
(17, N'Đường cát trắng', 'kg', 20, 0, 'normal', '2026-09-25 12:00:36', '2026-09-25 12:00:36'),
(18, N'Siro Đào', 'chai', 4, 0, 'normal', '2026-09-25 12:00:36', '2026-09-25 12:00:36'),
(19, N'Phô mai dẻo', 'g', 0, 0, 'normal', '2026-09-25 13:15:27', '2026-09-25 13:15:27');
SET IDENTITY_INSERT [ingredients] OFF;

-- --- USERS ---
SET IDENTITY_INSERT [users] ON;
INSERT INTO [users] ([id], [name], [email], [phone], [role], [status], [points], [tier], [email_verified_at], [password], [remember_token], [created_at], [updated_at]) VALUES
(1, N'Quản trị viên', 'admin@trasua.test', '0900000001', 'admin', 1, 0, N'Đồng', NULL, '$2y$12$q2Uqh9gS0CFU5ydRnZjcOOp6Juklp.mtflL292EU/QWXLJ1.P0LaG', NULL, '2026-09-16 20:32:50', '2026-09-16 20:32:50'),
(2, N'Nhân viên Bán hàng', 'pos@trasua.test', '0900000002', 'pos', 1, 0, N'Đồng', NULL, '$2y$12$wmBqjDnQz1L8jVSXCc8sYubF3MHavvP0VeZETyV4twOLwRC.wuAvi', NULL, '2026-09-16 20:32:51', '2026-09-16 20:32:51'),
(3, N'Nhân viên Kho', 'kho@trasua.test', '0900000003', 'kho', 1, 0, N'Đồng', NULL, '$2y$12$v63nN.4lFI6uWQhDLbiWe.W/Fr0Kb.GsyqhlZmSaShBJiRyAYz89W', NULL, '2026-09-16 20:32:51', '2026-09-16 20:32:51'),
(4, N'Khách hàng Demo', 'customer@trasua.test', '0900000004', 'customer', 1, 16, N'Đồng', NULL, '$2y$12$IyEoZKbYlp7iTr3NNLhfR.sIVwEhKMUtBR2JJNjHJ3A0Jr6TqVBYG', NULL, '2026-09-16 20:32:51', '2026-09-25 03:19:29'),
(5, N'Clyde Schinner', 'natasha.brown@example.net', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'C0fYiR7ukX', '2026-09-16 20:32:51', '2026-09-16 20:32:51'),
(6, N'Audrey Friesen', 'ocarter@example.org', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'wi2aDvWhng', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(7, N'Dr. Federico D''Amore', 'kayden94@example.com', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'yzXBBHqlWj', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(8, N'Jevon Green', 'iweimann@example.org', NULL, 'customer', 1, 8, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'QFvqGILjfc', '2026-09-16 20:32:52', '2026-09-25 03:24:58'),
(9, N'Jorge Sanford DVM', 'batz.clay@example.com', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'qrHWIw6WUd', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(10, N'Blair Kris', 'orval.douglas@example.net', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'aggiSGcoH7', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(11, N'Cheyanne Renner', 'bmarvin@example.com', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'DypHCcP0ov', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(12, N'Anastacio Crona', 'chasity.larkin@example.net', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'TuOcx4ATTu', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(13, N'Dr. Dameon Considine Jr.', 'jaime52@example.net', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'joUhp603cu', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(14, N'Miss Camila Koss Sr.', 'arely53@example.org', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'QPV2XuqQxd', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(15, N'Kory Johnston', 'hemard@example.org', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'cVbADWUq63', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(16, N'Mrs. Ana Funk I', 'oswaldo.ortiz@example.com', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'bFwh4Q8eQe', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(17, N'Deshawn Mayert', 'dave96@example.org', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'vWGVMJPOr2', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(18, N'Jedediah Botsford', 'marisol.hodkiewicz@example.com', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'vUZIg5vCWR', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(19, N'Dayna Cremin', 'mraz.dorothy@example.com', NULL, 'customer', 1, 0, N'Đồng', '2026-09-16 20:32:51', '$2y$12$oMqEdszB9XCENWZT75N0T.UYwFzgvQwSaFs9ykXdf.PEGUX5YCoG6', 'JSBx4TvCK0', '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(20, N'Duc Anh Test', 'testDA@gmail.com', '0123654987', 'customer', 1, 0, N'Đồng', NULL, '$2y$12$DvqkJXwFs9o3tuvxdxwYVeZAzkEwd0Nd2ARHrqP/MXr/XZvDGA.o2', NULL, '2026-09-16 20:56:45', '2026-09-16 20:56:45'),
(22, N'Đức Anh', 'a@gmail.com', '0111222333', 'pos', 1, 0, N'Đồng', NULL, '$2y$12$rE5srtzBNJANPoKS6xNNgOeuUkonWOkmVHncU8Qx8AmsXhFP0X1aG', NULL, '2026-09-24 19:30:05', '2026-09-24 19:31:47'),
(23, N'khachhhtesst', 'khachhang@test.com', '03214563777', 'customer', 1, 7, N'Đồng', NULL, '$2y$12$AryVKjqn.lAi4HO2VcKSPOa1bty0W2hder8Zp5mv0jM.3w4BbWWsK', NULL, '2026-09-25 05:19:33', '2026-09-25 05:29:14'),
(24, N'A', 'daa@t.com', '0123666555', 'customer', 1, 0, N'Đồng', NULL, '$2y$12$vLyW5lhpPUTkN6SkeBZRiONz91SWq3V0yMN/P3UToA4ubTk4CwNk2', NULL, '2026-09-25 13:29:34', '2026-09-25 13:29:34'),
(25, N'Đưccs Anhss', 'aaa@gmail.com', '0987789987', 'customer', 1, 10, N'Đồng', NULL, '$2y$12$SgEk1Keuau4CZDN5P9cQC.d.kpNYshdIMLrYEoUcRHyyqOQFpPSI.', NULL, '2026-09-25 16:20:32', '2026-09-25 16:20:32');
SET IDENTITY_INSERT [users] OFF;

-- --- VOUCHERS ---
SET IDENTITY_INSERT [vouchers] ON;
INSERT INTO [vouchers] ([id], [code], [name], [discount_type], [discount_value], [max_discount], [min_order_value], [required_tier], [usage_limit], [used_count], [expires_at], [is_active], [created_at], [updated_at]) VALUES
(1, 'TET2026', N'Mừng đón Tết', 'percent', 10, 40000, 50000, NULL, 50, 0, '2026-09-30 18:56:00', 1, '2026-09-25 04:56:41', '2026-09-25 13:13:38'),
(2, 'CHAO2026', N'Chào năm 2026', 'fixed', 15000, NULL, 50000, NULL, NULL, 4, NULL, 1, '2026-09-25 05:15:48', '2026-09-25 16:20:32'),
(3, 'vipvang', N'dành cho khách vip bậc vàng trở lên', 'percent', 20, 50000, 50000, N'Vàng', NULL, 0, NULL, 1, '2026-09-25 05:16:48', '2026-09-25 05:16:48');
SET IDENTITY_INSERT [vouchers] OFF;

-- --- TOPPINGS ---
SET IDENTITY_INSERT [toppings] ON;
INSERT INTO [toppings] ([id], [name], [price], [ingredient_id], [amount_per_serving], [is_active], [created_at], [updated_at]) VALUES
(1, N'Trân châu đen', 5000, 6, 30, 1, '2026-09-24 05:03:15', '2026-09-24 05:03:15'),
(2, N'Thạch trái cây', 5000, NULL, NULL, 1, '2026-09-24 05:03:21', '2026-09-24 05:03:21'),
(3, N'Trân châu trắng', 5000, 14, 20, 1, '2026-09-25 10:40:21', '2026-09-25 10:40:21'),
(4, N'Phô mai dẻo', 7000, 19, 20, 1, '2026-09-25 13:15:39', '2026-09-25 13:15:39');
SET IDENTITY_INSERT [toppings] OFF;

-- --- PRODUCTS ---
SET IDENTITY_INSERT [products] ON;
INSERT INTO [products] ([id], [category_id], [name], [image], [description], [is_active], [created_at], [updated_at]) VALUES
(1, 1, N'Trà Sữa Truyền Thống', 'products/4R9cQIDGAw1zNISpAaLgVmt5H7HMMEfE25xOQ2OZ.jpg', N'Vị trà đen đậm đà hòa quyện cùng sữa béo ngậy.', 1, '2026-09-16 20:32:52', '2026-09-25 13:08:02'),
(2, 1, N'Trà Sữa Trân Châu Đường Đen', 'products/3Rg6xxcMFdpvq47UtjfoNDPUN6jeAL0C9LYMXK92.webp', N'Trân châu dẻo dai, thơm mùi caramel đường đen.', 1, '2026-09-16 20:32:52', '2026-09-25 13:23:30'),
(3, 1, N'Trà Sữa Matcha', 'products/KylLi7oTWtYkHk58OzfWrxvlUhKEu0AQiRv2qlve.webp', N'Matcha Nhật Bản nguyên chất, béo nhẹ vị trà xanh.', 1, '2026-09-16 20:32:52', '2026-09-25 13:08:21'),
(4, 2, N'Hồng Trà Đào Cam Sả', 'products/nvsHhrnn5FVSmNV8yUmCeIeseLwm6Ahrccaa7dzN.jpg', N'Hồng trà thanh mát kết hợp đào, cam và sả tươi.', 1, '2026-09-16 20:32:52', '2026-09-25 13:20:36'),
(5, 2, N'Trà Vải', 'products/cRVg26MD2JgBIugHAIKK1jLYaqEefz3zweIYV6Cl.jpg', N'Trà xanh thơm hương vải chín ngọt dịu.', 1, '2026-09-16 20:32:53', '2026-09-25 13:08:39'),
(6, 3, N'Cà Phê Sữa Đá', 'products/ivlRlzlndUzsfCYVCrUcCTAmYcWA64tRaZxZRh13.webp', N'Cà phê phin truyền thống pha cùng sữa đặc.', 1, '2026-09-16 20:32:53', '2026-09-25 13:23:49'),
(7, 3, N'Bạc Xỉu', 'products/i0VQtyy1YZQjn998VaBbguEckDYDyR77L18uTrwj.webp', N'Nhiều sữa, ít cà phê — vị ngọt béo dịu nhẹ.', 1, '2026-09-16 20:32:53', '2026-09-25 13:23:12'),
(8, 4, N'Socola Đá Xay', 'products/CieovRRKyGulovsjeBlDOxO0TY7gTqRsXg8F8rl2.webp', N'Đá xay mịn hòa quyện cùng socola nguyên chất.', 1, '2026-09-16 20:32:53', '2026-09-25 13:22:52'),
(9, 4, N'Dâu Tây Đá Xay', 'products/J5FIT2laIfQgu5ZruKQyZsnsvi8KIxu0iMZkbyvv.jpg', N'Dâu tây tươi xay mịn, chua ngọt sảng khoái.', 1, '2026-09-16 20:32:53', '2026-09-25 13:20:11'),
(11, 1, N'Trà sữa socola', 'products/mgfPZtf1sHsOLBmYOEqF57JTgyHscby12thBiq5d.webp', N'Trà sữa béo ngậy kết hợp với sô cô la lôi cuốn', 1, '2026-09-21 09:18:56', '2026-09-25 13:22:07'),
(12, 5, N'Matcha Machiato', 'products/1790588816_matchalatte4.jpd.jpg', N'Thơm trà và béo sữa', 1, '2026-09-23 19:02:35', '2026-09-28 02:46:56'),
(13, 1, N'Trà sữa test phân trang', NULL, 'sad', 0, '2026-09-24 19:08:25', '2026-09-25 17:21:36'),
(14, 1, 'test', NULL, '2', 1, '2026-09-25 13:25:20', '2026-09-25 13:25:20');
SET IDENTITY_INSERT [products] OFF;

-- --- PRODUCT_VARIANTS ---
SET IDENTITY_INSERT [product_variants] ON;
INSERT INTO [product_variants] ([id], [product_id], [size], [price], [created_at], [updated_at]) VALUES
(1, 1, 'S', 25000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(2, 1, 'M', 29000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(3, 1, 'L', 33000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(4, 2, 'S', 32000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(5, 2, 'M', 36000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(6, 2, 'L', 40000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(7, 3, 'S', 32000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(8, 3, 'M', 36000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(9, 3, 'L', 40000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(10, 4, 'S', 29000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(11, 4, 'M', 33000, '2026-09-16 20:32:52', '2026-09-16 20:32:52'),
(12, 4, 'L', 37000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(13, 5, 'S', 27000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(14, 5, 'M', 31000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(15, 5, 'L', 35000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(16, 6, 'S', 25000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(17, 6, 'M', 29000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(18, 6, 'L', 33000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(19, 7, 'S', 27000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(20, 7, 'M', 31000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(21, 7, 'L', 35000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(22, 8, 'S', 35000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(23, 8, 'M', 39000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(24, 8, 'L', 43000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(25, 9, 'S', 35000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(26, 9, 'M', 39000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(27, 9, 'L', 43000, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(31, 11, 'S', 25000, '2026-09-21 09:18:56', '2026-09-21 09:18:56'),
(32, 11, 'M', 30000, '2026-09-21 09:18:56', '2026-09-21 09:18:56'),
(33, 11, 'L', 35000, '2026-09-21 09:18:56', '2026-09-21 09:18:56'),
(34, 12, 'S', 25000, '2026-09-23 19:02:35', '2026-09-23 19:02:35'),
(35, 12, 'M', 30000, '2026-09-23 19:02:35', '2026-09-23 19:02:35'),
(36, 12, 'L', 35000, '2026-09-23 19:02:35', '2026-09-23 19:02:35'),
(37, 13, 'S', 123333, '2026-09-24 19:08:25', '2026-09-24 19:08:25'),
(38, 13, 'M', 233333, '2026-09-24 19:08:25', '2026-09-24 19:08:25'),
(39, 13, 'L', 2333666, '2026-09-24 19:08:25', '2026-09-24 19:08:25'),
(40, 14, 'S', 12333, '2026-09-25 13:25:20', '2026-09-25 13:25:20'),
(41, 14, 'M', 33333, '2026-09-25 13:25:20', '2026-09-25 13:25:20'),
(42, 14, 'L', 333333, '2026-09-25 13:25:20', '2026-09-25 13:25:20');
SET IDENTITY_INSERT [product_variants] OFF;

-- --- RECIPE_DETAILS ---
INSERT INTO [recipe_details] ([variant_id], [ingredient_id], [amount]) VALUES
(1, 1, 8), (1, 4, 100), (1, 5, 20), (1, 7, 15), (1, 8, 150),
(2, 1, 9.6), (2, 4, 120), (2, 5, 24), (2, 7, 18), (2, 8, 180),
(3, 1, 12), (3, 4, 150), (3, 5, 30), (3, 7, 22.5), (3, 8, 225),
(4, 1, 8), (4, 4, 100), (4, 6, 50), (4, 7, 20), (4, 8, 150),
(5, 1, 9.6), (5, 4, 120), (5, 6, 60), (5, 7, 24), (5, 8, 180),
(6, 1, 12), (6, 4, 150), (6, 6, 75), (6, 7, 30), (6, 8, 225),
(7, 3, 12), (7, 4, 100), (7, 5, 20), (7, 7, 15), (7, 8, 150),
(8, 3, 14.4), (8, 4, 120), (8, 5, 24), (8, 7, 18), (8, 8, 180),
(9, 3, 18), (9, 4, 150), (9, 5, 30), (9, 7, 22.5), (9, 8, 225),
(10, 1, 6), (10, 7, 20), (10, 8, 180),
(11, 1, 7.2), (11, 7, 24), (11, 8, 216),
(12, 1, 9), (12, 7, 30), (12, 8, 270),
(13, 2, 6), (13, 7, 20), (13, 8, 180),
(14, 2, 7.2), (14, 7, 24), (14, 8, 216),
(15, 2, 9), (15, 7, 30), (15, 8, 270),
(16, 4, 40), (16, 7, 10), (16, 8, 150), (16, 9, 20),
(17, 4, 48), (17, 7, 12), (17, 8, 180), (17, 9, 24),
(18, 4, 60), (18, 7, 15), (18, 8, 225), (18, 9, 30),
(19, 4, 120), (19, 7, 15), (19, 8, 150), (19, 9, 10),
(20, 4, 144), (20, 7, 18), (20, 8, 180), (20, 9, 12),
(21, 4, 180), (21, 7, 22.5), (21, 8, 225), (21, 9, 15),
(22, 4, 100), (22, 7, 20), (22, 8, 200), (22, 10, 25),
(23, 4, 120), (23, 7, 24), (23, 8, 240), (23, 10, 30),
(24, 4, 150), (24, 7, 30), (24, 8, 300), (24, 10, 37.5),
(25, 4, 80), (25, 7, 15), (25, 8, 200), (25, 11, 80),
(26, 4, 96), (26, 7, 18), (26, 8, 240), (26, 11, 96),
(27, 4, 120), (27, 7, 22.5), (27, 8, 300), (27, 11, 120),
(31, 8, 20), (31, 15, 0.5), (31, 17, 0.02),
(32, 8, 24), (32, 15, 0.6), (32, 17, 0.02),
(33, 8, 30), (33, 15, 0.75), (33, 17, 0.03),
(34, 3, 6), (34, 4, 30), (34, 12, 20), (34, 13, 10),
(35, 3, 7.2), (35, 4, 36), (35, 12, 24), (35, 13, 12),
(36, 3, 9), (36, 4, 45), (36, 12, 30), (36, 13, 15),
(37, 10, 1),
(38, 10, 1.2),
(39, 10, 1.5),
(40, 5, 20),
(41, 5, 24),
(42, 5, 30);

-- --- ORDERS ---
SET IDENTITY_INSERT [orders] ON;
INSERT INTO [orders] ([id], [customer_id], [staff_id], [subtotal], [shipping_fee], [discount_amount], [final_total], [payment_method], [payment_status], [order_status], [order_type], [shipping_name], [shipping_phone], [shipping_address], [created_at], [updated_at]) VALUES
(1, 13, NULL, 49000, 15000, 0, 64000, 'transfer', 'paid', 'completed', 'online', N'Dr. Dameon Considine Jr.', NULL, N'405 Terrill Isle Suite 753\nNew Jazmyn, MS 74267', '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(2, 5, NULL, 68000, 15000, 0, 83000, 'transfer', 'paid', 'shipping', 'online', N'Clyde Schinner', NULL, N'1845 Watsica Stravenue Apt. 758\nSouth Ulisesfurt, NM 89605', '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(3, 11, NULL, 228000, 15000, 0, 243000, 'transfer', 'paid', 'completed', 'online', N'Cheyanne Renner', NULL, N'34664 Stroman Landing\nSouth Noelborough, IA 98800', '2026-09-16 20:32:53', '2026-09-25 03:09:22'),
(4, 8, NULL, 75000, 15000, 10000, 80000, 'transfer', 'paid', 'completed', 'online', N'Jevon Green', NULL, N'6993 Cormier Falls\nEast Marvin, DE 92677', '2026-09-16 20:32:53', '2026-09-25 03:24:58'),
(5, 14, NULL, 177000, 15000, 0, 192000, 'transfer', 'paid', 'pending', 'online', N'Miss Camila Koss Sr.', NULL, N'68077 Orlo Park\nEast Nevaland, IL 81267', '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(6, 4, NULL, 149000, 15000, 0, 164000, 'transfer', 'paid', 'completed', 'online', N'Khách hàng Demo', '0900000004', N'737 Rosalind Junctions\nMohrtown, NH 07759', '2026-09-16 20:32:54', '2026-09-25 03:19:29'),
(7, 15, NULL, 123000, 15000, 0, 138000, 'transfer', 'paid', 'completed', 'online', N'Kory Johnston', NULL, N'4253 Monahan Shoals\nPort Geo, KY 52788-8507', '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(8, 19, NULL, 35000, 15000, 0, 50000, 'transfer', 'paid', 'preparing', 'online', N'Dayna Cremin', NULL, N'54929 Will Meadows Apt. 015\nPort Beatrice, OK 83487', '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(9, NULL, 2, 211000, 0, 10000, 201000, 'cash', 'paid', 'shipping', 'in_store', NULL, NULL, NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(10, NULL, 2, 218000, 0, 0, 218000, 'cash', 'paid', 'shipping', 'in_store', NULL, NULL, NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(11, NULL, 2, 70000, 0, 10000, 60000, 'cash', 'paid', 'preparing', 'in_store', NULL, NULL, NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(12, NULL, 2, 113000, 0, 10000, 103000, 'cash', 'paid', 'pending', 'in_store', NULL, NULL, NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(13, NULL, 2, 39000, 0, 10000, 29000, 'cash', 'paid', 'shipping', 'in_store', NULL, NULL, NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(14, NULL, 2, 62000, 0, 0, 62000, 'cash', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(15, NULL, NULL, 39000, 15000, 0, 54000, 'cash', 'paid', 'completed', 'online', N'Đức Anh', '0321456987', N'Thành phố Hồ Chí Minh', '2026-09-20 23:50:18', '2026-09-25 13:28:40'),
(16, 4, NULL, 27000, 15000, 0, 42000, 'cash', 'paid', 'completed', 'online', N'Khách hàng Demo', '0900000004', 'ads', '2026-09-22 07:12:25', '2026-09-25 03:16:08'),
(17, NULL, 2, 27000, 0, 0, 27000, 'cash', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-22 23:29:29', '2026-09-22 23:29:29'),
(18, NULL, 2, 27000, 0, 0, 27000, 'transfer', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-23 04:33:54', '2026-09-23 04:33:54'),
(19, NULL, NULL, 36000, 15000, 0, 51000, 'cash', 'paid', 'completed', 'online', N'Đức Anh', '033123998', N'Hồ Chí Minh', '2026-09-24 05:06:14', '2026-09-25 03:15:06'),
(20, NULL, 2, 30000, 0, 0, 30000, 'cash', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-24 18:20:14', '2026-09-24 18:20:14'),
(21, 23, NULL, 73000, 15000, 15000, 73000, 'transfer', 'unpaid', 'pending', 'online', 'khachhhtesst', '03214563777', N'123 Đường ABC', '2026-09-25 05:21:29', '2026-09-25 05:21:29'),
(22, 23, NULL, 73000, 15000, 15000, 73000, 'cash', 'paid', 'completed', 'online', 'khachhhtesst', '03214563777', '123', '2026-09-25 05:23:26', '2026-09-25 05:29:14'),
(23, NULL, 2, 40000, 0, 0, 40000, 'cash', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-25 05:28:18', '2026-09-25 05:28:18'),
(24, 4, NULL, 62000, 15000, 0, 77000, 'cash', 'unpaid', 'shipping', 'online', N'Khách hàng Demo', '0900000004', N'Hồ Chí Minh', '2026-09-25 10:47:37', '2026-09-25 11:41:43'),
(25, NULL, 2, 30000, 0, 0, 30000, 'cash', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-25 11:46:16', '2026-09-25 11:46:16'),
(26, NULL, 2, 30000, 0, 0, 30000, 'cash', 'paid', 'completed', 'in_store', NULL, NULL, NULL, '2026-09-25 11:47:53', '2026-09-25 11:47:53'),
(27, NULL, 2, 25000, 0, 0, 25000, 'cash', 'paid', 'completed', 'in_store', N'Khách lẻ', NULL, NULL, '2026-09-25 13:26:13', '2026-09-25 13:26:13'),
(28, 24, NULL, 80000, 15000, 15000, 80000, 'transfer', 'unpaid', 'pending', 'online', 'A', '0123666555', 'H', '2026-09-25 13:30:31', '2026-09-25 13:30:31'),
(29, 24, NULL, 35000, 15000, 0, 50000, 'cash', 'unpaid', 'pending', 'online', 'A', '0123666555', 'A', '2026-09-25 13:41:20', '2026-09-25 13:41:20'),
(30, NULL, NULL, 52000, 15000, 0, 67000, 'cash', 'unpaid', 'pending', 'online', N'Đức Anh', '0321456987', N'Hồ Chí Minh', '2026-09-25 15:12:49', '2026-09-25 15:12:49'),
(31, 25, 2, 65000, 0, 15000, 50000, 'cash', 'paid', 'completed', 'in_store', N'Đưccs Anhss', '0987789987', NULL, '2026-09-25 16:20:32', '2026-09-25 16:20:32');
SET IDENTITY_INSERT [orders] OFF;

-- --- ORDER_ITEMS ---
SET IDENTITY_INSERT [order_items] ON;
INSERT INTO [order_items] ([id], [order_id], [variant_id], [quantity], [unit_price], [sugar_level], [ice_option], [note], [created_at], [updated_at]) VALUES
(1, 1, 27, 1, 43000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(2, 2, 2, 2, 29000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(3, 3, 5, 2, 36000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(4, 3, 7, 2, 32000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(5, 3, 9, 2, 40000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(6, 4, 11, 1, 33000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(7, 4, 12, 1, 37000, '100', 'normal', NULL, '2026-09-16 20:32:53', '2026-09-16 20:32:53'),
(8, 5, 14, 2, 31000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(9, 5, 17, 1, 29000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(10, 5, 22, 2, 35000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(11, 6, 4, 2, 32000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(12, 6, 11, 1, 33000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(13, 6, 12, 1, 37000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(14, 7, 19, 1, 27000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(15, 7, 24, 2, 43000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(16, 8, 22, 1, 35000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(17, 9, 6, 2, 40000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(18, 9, 25, 2, 35000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(19, 9, 27, 1, 43000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(20, 10, 17, 2, 29000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(21, 10, 22, 2, 35000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(22, 10, 25, 2, 35000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(23, 11, 25, 2, 35000, '100', 'normal', NULL, '2026-09-16 20:32:54', '2026-09-16 20:32:54'),
(24, 12, 6, 2, 40000, '100', 'normal', NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(25, 12, 13, 1, 27000, '100', 'normal', NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(26, 13, 23, 1, 39000, '100', 'normal', NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(27, 14, 1, 1, 25000, '100', 'normal', NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(28, 14, 7, 1, 32000, '100', 'normal', NULL, '2026-09-16 20:32:55', '2026-09-16 20:32:55'),
(29, 15, 26, 1, 39000, '100', 'normal', NULL, '2026-09-20 23:50:18', '2026-09-20 23:50:18'),
(30, 16, 19, 1, 27000, '100', 'normal', NULL, '2026-09-22 07:12:25', '2026-09-22 07:12:25'),
(31, 17, 19, 1, 27000, '100', 'normal', NULL, '2026-09-22 23:29:29', '2026-09-22 23:29:29'),
(32, 18, 13, 1, 27000, '100', 'normal', NULL, '2026-09-23 04:33:54', '2026-09-23 04:33:54'),
(33, 19, 8, 1, 36000, '50', 'less', N'giao trước cổng khu chung cư đợi tôi ra nhận hàng', '2026-09-24 05:06:14', '2026-09-24 05:06:14'),
(34, 20, 1, 1, 25000, '100', 'normal', NULL, '2026-09-24 18:20:14', '2026-09-24 18:20:14'),
(35, 21, 6, 1, 40000, '70', 'less', N'Nhìu chân trâu nha', '2026-09-25 05:21:29', '2026-09-25 05:21:29'),
(36, 21, 11, 1, 33000, '100', 'normal', NULL, '2026-09-25 05:21:29', '2026-09-25 05:21:29'),
(37, 22, 6, 1, 40000, '70', 'less', NULL, '2026-09-25 05:23:26', '2026-09-25 05:23:26'),
(38, 22, 11, 1, 33000, '100', 'normal', NULL, '2026-09-25 05:23:26', '2026-09-25 05:23:26'),
(39, 23, 9, 1, 40000, '30', 'none', NULL, '2026-09-25 05:28:18', '2026-09-25 05:28:18'),
(40, 24, 31, 1, 25000, '100', 'normal', NULL, '2026-09-25 10:47:37', '2026-09-25 10:47:37'),
(41, 24, 13, 1, 27000, '100', 'normal', NULL, '2026-09-25 10:47:37', '2026-09-25 10:47:37'),
(42, 25, 1, 1, 25000, '100', 'normal', NULL, '2026-09-25 11:46:16', '2026-09-25 11:46:16'),
(43, 26, 1, 1, 25000, '100', 'normal', NULL, '2026-09-25 11:47:53', '2026-09-25 11:47:53'),
(44, 27, 1, 1, 25000, '100', 'normal', NULL, '2026-09-25 13:26:13', '2026-09-25 13:26:13'),
(45, 28, 15, 1, 35000, '100', 'normal', NULL, '2026-09-25 13:30:31', '2026-09-25 13:30:31'),
(46, 28, 25, 1, 35000, '100', 'normal', NULL, '2026-09-25 13:30:31', '2026-09-25 13:30:31'),
(47, 29, 36, 1, 35000, '100', 'normal', NULL, '2026-09-25 13:41:20', '2026-09-25 13:41:20'),
(48, 30, 13, 1, 27000, '100', 'normal', NULL, '2026-09-25 15:12:49', '2026-09-25 15:12:49'),
(49, 30, 16, 1, 25000, '100', 'normal', NULL, '2026-09-25 15:12:50', '2026-09-25 15:12:50'),
(50, 31, 9, 1, 40000, '100', 'normal', NULL, '2026-09-25 16:20:32', '2026-09-25 16:20:32'),
(51, 31, 1, 1, 25000, '100', 'none', NULL, '2026-09-25 16:20:32', '2026-09-25 16:20:32');
SET IDENTITY_INSERT [order_items] OFF;

-- --- ORDER_ITEM_TOPPINGS ---
SET IDENTITY_INSERT [order_item_toppings] ON;
INSERT INTO [order_item_toppings] ([id], [order_item_id], [topping_id], [topping_name], [price]) VALUES
(1, 1, NULL, N'Pudding trứng', 6000), (2, 2, NULL, N'Trân châu đen', 5000), (3, 4, NULL, N'Pudding trứng', 6000),
(4, 6, NULL, N'Trân châu đen', 5000), (5, 8, NULL, N'Thạch trái cây', 5000), (6, 9, NULL, N'Pudding trứng', 6000),
(7, 11, NULL, N'Trân châu đen', 5000), (8, 12, NULL, N'Thạch trái cây', 5000), (9, 15, NULL, N'Trân châu đen', 5000),
(10, 18, NULL, N'Pudding trứng', 6000), (11, 19, NULL, N'Pudding trứng', 6000), (12, 20, NULL, N'Trân châu đen', 5000),
(13, 22, NULL, N'Thạch trái cây', 5000), (14, 25, NULL, N'Pudding trứng', 6000), (15, 27, NULL, N'Thạch trái cây', 5000),
(16, 34, 2, N'Thạch trái cây', 5000), (17, 40, 3, N'Trân châu trắng', 5000), (18, 41, 3, N'Trân châu trắng', 5000),
(19, 42, 1, N'Trân châu đen', 5000), (20, 43, 3, N'Trân châu trắng', 5000), (21, 45, 3, N'Trân châu trắng', 5000),
(22, 46, 3, N'Trân châu trắng', 5000);
SET IDENTITY_INSERT [order_item_toppings] OFF;

-- --- INVENTORY_TRANSACTIONS ---
SET IDENTITY_INSERT [inventory_transactions] ON;
INSERT INTO [inventory_transactions] ([id], [ingredient_id], [order_id], [quantity_changed], [total_cost], [unit_cost], [transaction_type], [note], [created_at]) VALUES
(1, 1, NULL, 5000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(2, 2, NULL, 4000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(3, 3, NULL, 2000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(4, 4, NULL, 10000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(5, 5, NULL, 6000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(6, 6, NULL, 8000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(7, 7, NULL, 10000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(8, 8, NULL, 50000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(9, 9, NULL, 3000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(10, 10, NULL, 200, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(11, 11, NULL, 3000, NULL, NULL, 'import', N'Nhập kho ban đầu', '2026-09-16 20:32:52'),
(12, 4, 1, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #1', '2026-09-16 20:32:53'),
(13, 7, 1, -22.5, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #1', '2026-09-16 20:32:53'),
(14, 8, 1, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #1', '2026-09-16 20:32:53'),
(15, 11, 1, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #1', '2026-09-16 20:32:53'),
(16, 1, 2, -19.2, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #2', '2026-09-16 20:32:53'),
(17, 4, 2, -240, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #2', '2026-09-16 20:32:53'),
(18, 5, 2, -48, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #2', '2026-09-16 20:32:53'),
(19, 7, 2, -36, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #2', '2026-09-16 20:32:53'),
(20, 8, 2, -360, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #2', '2026-09-16 20:32:53'),
(21, 1, 3, -19.2, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(22, 4, 3, -240, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(23, 6, 3, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(24, 7, 3, -48, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(25, 8, 3, -360, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(26, 3, 3, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(27, 4, 3, -200, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(28, 5, 3, -40, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(29, 7, 3, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(30, 8, 3, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(31, 3, 3, -36, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(32, 4, 3, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(33, 5, 3, -60, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(34, 7, 3, -45, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(35, 8, 3, -450, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #3', '2026-09-16 20:32:53'),
(36, 1, 4, -7.2, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #4', '2026-09-16 20:32:53'),
(37, 7, 4, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #4', '2026-09-16 20:32:53'),
(38, 8, 4, -216, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #4', '2026-09-16 20:32:53'),
(39, 1, 4, -9, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #4', '2026-09-16 20:32:53'),
(40, 7, 4, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #4', '2026-09-16 20:32:53'),
(41, 8, 4, -270, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #4', '2026-09-16 20:32:54'),
(42, 2, 5, -14.4, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(43, 7, 5, -48, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(44, 8, 5, -432, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(45, 4, 5, -48, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(46, 7, 5, -12, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(47, 8, 5, -180, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(48, 9, 5, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(49, 4, 5, -200, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(50, 7, 5, -40, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(51, 8, 5, -400, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(52, 10, 5, -50, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #5', '2026-09-16 20:32:54'),
(53, 1, 6, -16, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(54, 4, 6, -200, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(55, 6, 6, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(56, 7, 6, -40, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(57, 8, 6, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(58, 1, 6, -7.2, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(59, 7, 6, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(60, 8, 6, -216, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(61, 1, 6, -9, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(62, 7, 6, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(63, 8, 6, -270, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #6', '2026-09-16 20:32:54'),
(64, 4, 7, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(65, 7, 7, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(66, 8, 7, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(67, 9, 7, -10, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(68, 4, 7, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(69, 7, 7, -60, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(70, 8, 7, -600, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(71, 10, 7, -75, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #7', '2026-09-16 20:32:54'),
(72, 4, 8, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #8', '2026-09-16 20:32:54'),
(73, 7, 8, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #8', '2026-09-16 20:32:54'),
(74, 8, 8, -200, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #8', '2026-09-16 20:32:54'),
(75, 10, 8, -25, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #8', '2026-09-16 20:32:54'),
(76, 1, 9, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(77, 4, 9, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(78, 6, 9, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(79, 7, 9, -60, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(80, 8, 9, -450, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(81, 4, 9, -160, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(82, 7, 9, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(83, 8, 9, -400, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(84, 11, 9, -160, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(85, 4, 9, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(86, 7, 9, -22.5, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(87, 8, 9, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(88, 11, 9, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #9', '2026-09-16 20:32:54'),
(89, 4, 10, -96, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(90, 7, 10, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(91, 8, 10, -360, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(92, 9, 10, -48, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(93, 4, 10, -200, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(94, 7, 10, -40, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(95, 8, 10, -400, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(96, 10, 10, -50, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(97, 4, 10, -160, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(98, 7, 10, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(99, 8, 10, -400, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(100, 11, 10, -160, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #10', '2026-09-16 20:32:54'),
(101, 4, 11, -160, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #11', '2026-09-16 20:32:55'),
(102, 7, 11, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #11', '2026-09-16 20:32:55'),
(103, 8, 11, -400, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #11', '2026-09-16 20:32:55'),
(104, 11, 11, -160, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #11', '2026-09-16 20:32:55'),
(105, 1, 12, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(106, 4, 12, -300, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(107, 6, 12, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(108, 7, 12, -60, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(109, 8, 12, -450, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(110, 2, 12, -6, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(111, 7, 12, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(112, 8, 12, -180, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #12', '2026-09-16 20:32:55'),
(113, 4, 13, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #13', '2026-09-16 20:32:55'),
(114, 7, 13, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #13', '2026-09-16 20:32:55'),
(115, 8, 13, -240, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #13', '2026-09-16 20:32:55'),
(116, 10, 13, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #13', '2026-09-16 20:32:55'),
(117, 1, 14, -8, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(118, 4, 14, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(119, 5, 14, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(120, 7, 14, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(121, 8, 14, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(122, 3, 14, -12, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(123, 4, 14, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(124, 5, 14, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(125, 7, 14, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(126, 8, 14, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #14', '2026-09-16 20:32:55'),
(127, 4, 15, -96, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-20 23:50:18'),
(128, 7, 15, -18, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-20 23:50:18'),
(129, 8, 15, -240, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-20 23:50:18'),
(130, 11, 15, -96, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-20 23:50:18'),
(131, 4, 16, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #16', '2026-09-22 07:12:25'),
(132, 7, 16, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #16', '2026-09-22 07:12:25'),
(133, 8, 16, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #16', '2026-09-22 07:12:25'),
(134, 9, 16, -10, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #16', '2026-09-22 07:12:25'),
(135, 4, 17, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #17', '2026-09-22 23:29:29'),
(136, 7, 17, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #17', '2026-09-22 23:29:29'),
(137, 8, 17, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #17', '2026-09-22 23:29:29'),
(138, 9, 17, -10, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #17', '2026-09-22 23:29:29'),
(139, 10, NULL, 900, NULL, NULL, 'import', N'Nhập kho thủ công', '2026-09-23 04:24:33'),
(140, 2, 18, -6, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #18', '2026-09-23 04:33:54'),
(141, 7, 18, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #18', '2026-09-23 04:33:54'),
(142, 8, 18, -180, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn tại quầy #18', '2026-09-23 04:33:54'),
(143, 10, NULL, 10, 100000, 10000.0000, 'import', N'Nhập kho theo hóa đơn', '2026-09-23 18:37:31'),
(144, 13, NULL, 1000, 500000, 500.0000, 'import', N'Nhập kho theo hóa đơn', '2026-09-23 19:07:40'),
(145, 12, NULL, 10000, 290000, 29.0000, 'import', N'Nhập kho theo hóa đơn', '2026-09-23 19:07:58'),
(146, 3, 19, -14.4, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #19', '2026-09-24 05:06:14'),
(147, 4, 19, -120, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #19', '2026-09-24 05:06:14'),
(148, 5, 19, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #19', '2026-09-24 05:06:14'),
(149, 7, 19, -9, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #19', '2026-09-24 05:06:14'),
(150, 8, 19, -90, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #19', '2026-09-24 05:06:14'),
(151, 1, 20, -8, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #20', '2026-09-24 18:20:14'),
(152, 4, 20, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #20', '2026-09-24 18:20:14'),
(153, 5, 20, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #20', '2026-09-24 18:20:14'),
(154, 7, 20, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #20', '2026-09-24 18:20:14'),
(155, 8, 20, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #20', '2026-09-24 18:20:14'),
(156, 1, 21, -12, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(157, 4, 21, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(158, 6, 21, -75, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(159, 7, 21, -21, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(160, 8, 21, -112.5, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(161, 1, 21, -7.2, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(162, 7, 21, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(163, 8, 21, -216, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #21', '2026-09-25 05:21:29'),
(164, 1, 22, -12, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(165, 4, 22, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(166, 6, 22, -75, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(167, 7, 22, -21, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(168, 8, 22, -112.5, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(169, 1, 22, -7.2, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(170, 7, 22, -24, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(171, 8, 22, -216, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #22', '2026-09-25 05:23:26'),
(172, 3, 23, -18, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #23', '2026-09-25 05:28:18'),
(173, 4, 23, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #23', '2026-09-25 05:28:18'),
(174, 5, 23, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #23', '2026-09-25 05:28:18'),
(175, 7, 23, -6.75, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #23', '2026-09-25 05:28:18'),
(176, 14, NULL, 10000, 300000, 30.0000, 'import', N'Nhập kho theo hóa đơn', '2026-09-25 10:41:05'),
(177, 14, 24, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 10:47:37'),
(178, 2, 24, -6, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 10:47:37'),
(179, 7, 24, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 10:47:37'),
(180, 8, 24, -180, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 10:47:37'),
(181, 14, 24, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 10:47:37'),
(182, 14, 24, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 11:41:38'),
(183, 2, 24, -6, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 11:41:38'),
(184, 7, 24, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 11:41:38'),
(185, 8, 24, -180, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 11:41:38'),
(186, 14, 24, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #24', '2026-09-25 11:41:38'),
(187, 1, 25, -8, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #25', '2026-09-25 11:46:16'),
(188, 4, 25, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #25', '2026-09-25 11:46:16'),
(189, 5, 25, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #25', '2026-09-25 11:46:16'),
(190, 7, 25, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #25', '2026-09-25 11:46:16'),
(191, 8, 25, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #25', '2026-09-25 11:46:16'),
(192, 6, 25, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #25', '2026-09-25 11:46:16'),
(193, 1, 26, -8, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #26', '2026-09-25 11:47:53'),
(194, 4, 26, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #26', '2026-09-25 11:47:53'),
(195, 5, 26, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #26', '2026-09-25 11:47:53'),
(196, 7, 26, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #26', '2026-09-25 11:47:53'),
(197, 8, 26, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #26', '2026-09-25 11:47:53'),
(198, 14, 26, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #26', '2026-09-25 11:47:53'),
(199, 15, NULL, 10, 1200000, 120000.0000, 'import', N'Nhập kho từ file CSV đợt 25/09/2026', '2026-09-25 19:00:36'),
(200, 16, NULL, 24, 528000, 22000.0000, 'import', N'Nhập kho từ file CSV đợt 25/09/2026', '2026-09-25 19:00:36'),
(201, 6, NULL, 5, 225000, 45000.0000, 'import', N'Nhập kho từ file CSV đợt 25/09/2026', '2026-09-25 19:00:36'),
(202, 17, NULL, 20, 400000, 20000.0000, 'import', N'Nhập kho từ file CSV đợt 25/09/2026', '2026-09-25 19:00:36'),
(203, 18, NULL, 4, 340000, 85000.0000, 'import', N'Nhập kho từ file CSV đợt 25/09/2026', '2026-09-25 19:00:36'),
(204, 1, 27, -8, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #27', '2026-09-25 13:26:13'),
(205, 4, 27, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #27', '2026-09-25 13:26:13'),
(206, 5, 27, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #27', '2026-09-25 13:26:13'),
(207, 7, 27, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #27', '2026-09-25 13:26:13'),
(208, 8, 27, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #27', '2026-09-25 13:26:13'),
(219, 4, 15, -96, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-25 13:28:32'),
(220, 7, 15, -18, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-25 13:28:32'),
(221, 8, 15, -240, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-25 13:28:32'),
(222, 11, 15, -96, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #15', '2026-09-25 13:28:32'),
(223, 2, 28, -9, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(224, 7, 28, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(225, 8, 28, -270, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(226, 14, 28, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(227, 4, 28, -80, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(228, 7, 28, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(229, 8, 28, -200, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(230, 11, 28, -80, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(231, 14, 28, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #28', '2026-09-25 13:30:31'),
(232, 3, 29, -9, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #29', '2026-09-25 13:41:20'),
(233, 4, 29, -45, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #29', '2026-09-25 13:41:20'),
(234, 12, 29, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #29', '2026-09-25 13:41:20'),
(235, 13, 29, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #29', '2026-09-25 13:41:20'),
(236, 2, 30, -6, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(237, 7, 30, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(238, 8, 30, -180, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(239, 4, 30, -40, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(240, 7, 30, -10, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(241, 8, 30, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(242, 9, 30, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #30', '2026-09-25 15:12:50'),
(243, 3, 31, -18, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(244, 4, 31, -150, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(245, 5, 31, -30, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(246, 7, 31, -22.5, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(247, 8, 31, -225, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(248, 1, 31, -8, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(249, 4, 31, -100, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(250, 5, 31, -20, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32'),
(251, 7, 31, -15, NULL, NULL, 'sale_deduction', N'Trừ kho tự động theo đơn #31', '2026-09-25 16:20:32');
SET IDENTITY_INSERT [inventory_transactions] OFF;
GO