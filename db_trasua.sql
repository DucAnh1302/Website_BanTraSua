-- Chạy đoạn code này để ép SSMS tạo lại toàn bộ Khóa Ngoại (Foreign Keys)

ALTER TABLE [inventory_transactions]
  ADD CONSTRAINT [fk_inv_trans_ingredient_id] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([id]);
GO

ALTER TABLE [inventory_transactions]
  ADD CONSTRAINT [fk_inv_trans_order_id] FOREIGN KEY ([order_id]) REFERENCES [orders] ([id]);
GO

ALTER TABLE [orders]
  ADD CONSTRAINT [fk_orders_customer_id] FOREIGN KEY ([customer_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [orders]
  ADD CONSTRAINT [fk_orders_staff_id] FOREIGN KEY ([staff_id]) REFERENCES [users] ([id]);
GO

ALTER TABLE [order_items]
  ADD CONSTRAINT [fk_order_items_order_id] FOREIGN KEY ([order_id]) REFERENCES [orders] ([id]);
GO

ALTER TABLE [order_items]
  ADD CONSTRAINT [fk_order_items_variant_id] FOREIGN KEY ([variant_id]) REFERENCES [product_variants] ([id]);
GO

ALTER TABLE [order_item_toppings]
  ADD CONSTRAINT [fk_order_item_toppings_order_item_id] FOREIGN KEY ([order_item_id]) REFERENCES [order_items] ([id]);
GO

ALTER TABLE [order_item_toppings]
  ADD CONSTRAINT [fk_order_item_toppings_topping_id] FOREIGN KEY ([topping_id]) REFERENCES [toppings] ([id]);
GO

ALTER TABLE [products]
  ADD CONSTRAINT [fk_products_category_id] FOREIGN KEY ([category_id]) REFERENCES [categories] ([id]);
GO

ALTER TABLE [product_variants]
  ADD CONSTRAINT [fk_product_variants_product_id] FOREIGN KEY ([product_id]) REFERENCES [products] ([id]);
GO

ALTER TABLE [recipe_details]
  ADD CONSTRAINT [fk_recipe_details_ingredient_id] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([id]);
GO

ALTER TABLE [recipe_details]
  ADD CONSTRAINT [fk_recipe_details_variant_id] FOREIGN KEY ([variant_id]) REFERENCES [product_variants] ([id]);
GO

ALTER TABLE [toppings]
  ADD CONSTRAINT [fk_toppings_ingredient_id] FOREIGN KEY ([ingredient_id]) REFERENCES [ingredients] ([id]);
GO