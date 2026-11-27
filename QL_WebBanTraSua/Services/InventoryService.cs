using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using QL_WebBanTraSua.Models;

namespace QL_WebBanTraSua.Services
{
    public static class InventoryService
    {
        public static double GetIngredientScaleFactor(ingredient ng, string sugarLevel, string iceOption)
        {
            if (ng == null) return 1.0;
            string role = (ng.consumption_role ?? "normal").Trim().ToLower();

            if (role == "sugar")
            {
                double pct;
                if (double.TryParse(sugarLevel, out pct))
                {
                    if (pct < 0) pct = 0;
                    if (pct > 100) pct = 100;
                    return pct / 100.0;
                }
                return 1.0;
            }

            if (role == "ice")
            {
                string ice = (iceOption ?? "normal").Trim().ToLower();
                if (ice == "less") return 0.5;
                if (ice == "none") return 0.0;
                return 1.0; // normal, separate
            }

            return 1.0;
        }

        public static bool IsToppingOutOfStock(topping tp)
        {
            if (tp == null || !tp.is_active) return true;
            if (tp.ingredient_id.HasValue && tp.amount_per_serving.HasValue && tp.amount_per_serving.Value > 0)
            {
                if (tp.ingredient != null && tp.ingredient.stock_quantity < tp.amount_per_serving.Value)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsVariantOutOfStock(product_variants variant)
        {
            if (variant == null || variant.recipe_details == null) return false;
            foreach (var rd in variant.recipe_details)
            {
                if (rd.ingredient != null && rd.amount > 0 && rd.ingredient.stock_quantity < rd.amount)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsProductOutOfStock(product prod)
        {
            if (prod == null || prod.product_variants == null || !prod.product_variants.Any())
            {
                return true;
            }
            return prod.product_variants.All(IsVariantOutOfStock);
        }

        /// <summary>
        /// Kiem tra tong luong nguyen lieu can dung cho gio hang. Tra ve ten nguyen lieu bi thieu neu khong du.
        /// </summary>
        public static string FindShortageForCart(db_trasuaEntities db, IEnumerable<CartItem> items)
        {
            if (items == null) return null;

            var needed = new Dictionary<long, double>();
            var ingredientCache = new Dictionary<long, ingredient>();

            var itemList = items.ToList();
            if (itemList.Count == 0) return null;

            var variantIds = itemList.Select(i => i.VariantId).Distinct().ToList();
            var variants = db.product_variants
                .Include(v => v.recipe_details.Select(r => r.ingredient))
                .Where(v => variantIds.Contains(v.id))
                .ToDictionary(v => v.id);

            var toppingIds = itemList
                .Where(i => i.Toppings != null)
                .SelectMany(i => i.Toppings.Select(t => t.Id))
                .Distinct()
                .ToList();

            var toppings = toppingIds.Count > 0
                ? db.toppings.Include(t => t.ingredient).Where(t => toppingIds.Contains(t.id)).ToDictionary(t => t.id)
                : new Dictionary<long, topping>();

            foreach (var line in itemList)
            {
                product_variants variant;
                if (variants.TryGetValue(line.VariantId, out variant) && variant.recipe_details != null)
                {
                    foreach (var rd in variant.recipe_details)
                    {
                        if (rd.ingredient == null) continue;
                        double factor = GetIngredientScaleFactor(rd.ingredient, line.SugarLevel, line.IceOption);
                        double qtyNeeded = rd.amount * line.Quantity * factor;
                        if (qtyNeeded <= 0) continue;

                        if (!needed.ContainsKey(rd.ingredient_id))
                        {
                            needed[rd.ingredient_id] = 0;
                            ingredientCache[rd.ingredient_id] = rd.ingredient;
                        }
                        needed[rd.ingredient_id] += qtyNeeded;
                    }
                }

                if (line.Toppings != null)
                {
                    foreach (var topItem in line.Toppings)
                    {
                        topping tp;
                        if (toppings.TryGetValue(topItem.Id, out tp) && tp.ingredient_id.HasValue && tp.amount_per_serving.HasValue && tp.ingredient != null)
                        {
                            double qtyNeeded = tp.amount_per_serving.Value * line.Quantity;
                            if (qtyNeeded <= 0) continue;

                            long ingId = tp.ingredient_id.Value;
                            if (!needed.ContainsKey(ingId))
                            {
                                needed[ingId] = 0;
                                ingredientCache[ingId] = tp.ingredient;
                            }
                            needed[ingId] += qtyNeeded;
                        }
                    }
                }
            }

            foreach (var kv in needed)
            {
                var ing = ingredientCache[kv.Key];
                if (ing.stock_quantity + 1e-6 < kv.Value)
                {
                    return ing.name + " (Cần " + Math.Round(kv.Value, 2) + " " + ing.unit + ", còn " + Math.Round(ing.stock_quantity, 2) + " " + ing.unit + ")";
                }
            }

            return null;
        }

        /// <summary>
        /// Tru kho tu dong cho 1 don hang da tao xong order_items va order_item_toppings.
        /// </summary>
        public static void DeductStockForOrder(db_trasuaEntities db, long orderId, bool allowShortage, string notePrefix = "Trừ kho tự động theo đơn #")
        {
            var items = db.order_items
                .Include(oi => oi.product_variants.recipe_details.Select(rd => rd.ingredient))
                .Include(oi => oi.order_item_toppings.Select(oit => oit.topping.ingredient))
                .Where(oi => oi.order_id == orderId)
                .ToList();

            foreach (var item in items)
            {
                if (item.product_variants != null && item.product_variants.recipe_details != null)
                {
                    foreach (var rd in item.product_variants.recipe_details)
                    {
                        if (rd.ingredient == null) continue;
                        double factor = GetIngredientScaleFactor(rd.ingredient, item.sugar_level, item.ice_option);
                        double amount = rd.amount * item.quantity * factor;
                        DeductIngredient(db, rd.ingredient, amount, orderId, allowShortage, notePrefix + orderId);
                    }
                }

                if (item.order_item_toppings != null)
                {
                    foreach (var oit in item.order_item_toppings)
                    {
                        var tp = oit.topping;
                        if (tp != null && tp.ingredient != null && tp.amount_per_serving.HasValue)
                        {
                            double amount = tp.amount_per_serving.Value * item.quantity;
                            DeductIngredient(db, tp.ingredient, amount, orderId, allowShortage, notePrefix + orderId + " (Topping: " + tp.name + ")");
                        }
                    }
                }
            }
        }

        private static void DeductIngredient(db_trasuaEntities db, ingredient ng, double amount, long orderId, bool allowShortage, string note)
        {
            if (amount <= 0 || ng == null) return;

            if (!allowShortage && ng.stock_quantity + 1e-6 < amount)
            {
                throw new InvalidOperationException("Nguyên liệu '" + ng.name + "' không đủ để hoàn tất đơn hàng.");
            }

            ng.stock_quantity = Math.Max(0, ng.stock_quantity - amount);
            ng.updated_at = DateTime.Now;

            db.inventory_transactions.Add(new inventory_transactions
            {
                ingredient_id = ng.id,
                order_id = orderId,
                quantity_changed = -amount,
                transaction_type = "sale_deduction",
                note = note,
                created_at = DateTime.Now
            });
        }

        /// <summary>
        /// Hoan kho chong trung lap khi huy don hang (dua tren cac giao dich da tru trong inventory_transactions).
        /// </summary>
        public static void RefundOrderStock(db_trasuaEntities db, order ord, string note = null)
        {
            if (ord == null) return;

            bool alreadyRefunded = db.inventory_transactions
                .Any(t => t.order_id == ord.id && t.transaction_type == "import_cancel" && t.quantity_changed > 0);
            if (alreadyRefunded) return;

            var deductions = db.inventory_transactions
                .Include(t => t.ingredient)
                .Where(t => t.order_id == ord.id && t.quantity_changed < 0)
                .ToList();

            string refundNote = string.IsNullOrWhiteSpace(note)
                ? "Hoàn kho tự động từ hủy đơn hàng #" + ord.id
                : note;

            foreach (var d in deductions)
            {
                double refundQty = Math.Abs(d.quantity_changed);
                if (refundQty <= 0) continue;

                var ng = d.ingredient ?? db.ingredients.Find(d.ingredient_id);
                if (ng != null)
                {
                    ng.stock_quantity += refundQty;
                    ng.updated_at = DateTime.Now;

                    db.inventory_transactions.Add(new inventory_transactions
                    {
                        ingredient_id = ng.id,
                        order_id = ord.id,
                        quantity_changed = refundQty,
                        transaction_type = "import_cancel",
                        note = refundNote,
                        created_at = DateTime.Now
                    });
                }
            }
        }

        public static string ResolveTierByPoints(int points)
        {
            if (points >= 1000) return "Kim Cương";
            if (points >= 500) return "Vàng";
            if (points >= 100) return "Bạc";
            return "Đồng";
        }

        public static int TierRank(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier)) return 0;
            string t = tier.Trim().ToLower();
            if (t == "kim cương" || t == "kim cuong") return 4;
            if (t == "vàng" || t == "vang") return 3;
            if (t == "bạc" || t == "bac") return 2;
            return 1; // Đồng
        }

        public static void AddLoyaltyPoints(user customer, decimal finalTotal)
        {
            if (customer == null) return;
            int earned = (int)Math.Floor(finalTotal / 10000m);
            if (earned <= 0) return;

            customer.points += earned;
            customer.tier = ResolveTierByPoints(customer.points);
            customer.updated_at = DateTime.Now;
        }

        public static void SubtractLoyaltyPoints(user customer, decimal finalTotal)
        {
            if (customer == null) return;
            int lost = (int)Math.Floor(finalTotal / 10000m);
            if (lost <= 0) return;

            customer.points = Math.Max(0, customer.points - lost);
            customer.tier = ResolveTierByPoints(customer.points);
            customer.updated_at = DateTime.Now;
        }

        public static voucher EvaluateVoucher(db_trasuaEntities db, string code, decimal subtotal, string customerTier, out decimal discount, out string error)
        {
            discount = 0m;
            error = null;

            if (string.IsNullOrWhiteSpace(code))
            {
                error = "Vui lòng nhập mã giảm giá.";
                return null;
            }

            string cleanCode = code.Trim().ToUpper();
            var v = db.vouchers.FirstOrDefault(x => x.code.ToUpper() == cleanCode && x.is_active);
            if (v == null)
            {
                error = "Mã giảm giá không tồn tại hoặc đã ngừng áp dụng.";
                return null;
            }

            if (v.expires_at.HasValue && v.expires_at.Value < DateTime.Now)
            {
                error = "Mã giảm giá '" + v.code + "' đã hết hạn sử dụng.";
                return null;
            }

            if (v.usage_limit.HasValue && v.used_count >= v.usage_limit.Value)
            {
                error = "Mã giảm giá '" + v.code + "' đã hết lượt sử dụng.";
                return null;
            }

            if (!string.IsNullOrWhiteSpace(v.required_tier))
            {
                if (string.IsNullOrWhiteSpace(customerTier) || TierRank(customerTier) < TierRank(v.required_tier))
                {
                    error = "Mã '" + v.code + "' chỉ áp dụng cho khách hàng từ hạng " + v.required_tier + " trở lên.";
                    return null;
                }
            }

            if (subtotal < v.min_order_value)
            {
                error = string.Format("Đơn hàng tối thiểu phải từ {0:N0}đ để áp dụng mã '{1}'.", v.min_order_value, v.code);
                return null;
            }

            if (v.discount_type == "percent")
            {
                discount = (subtotal * v.discount_value) / 100m;
                if (v.max_discount.HasValue && discount > v.max_discount.Value)
                {
                    discount = v.max_discount.Value;
                }
            }
            else
            {
                discount = v.discount_value;
            }

            if (discount > subtotal) discount = subtotal;
            return v;
        }

        public static void WriteAuditLog(
            db_trasuaEntities db,
            HttpRequestBase request,
            long? userId,
            string userEmail,
            string userRole,
            string action,
            string description,
            string targetType = null,
            string targetId = null,
            string oldValues = null,
            string newValues = null)
        {
            string ip = null;
            string ua = null;
            if (request != null)
            {
                ip = request.UserHostAddress;
                ua = request.UserAgent;
            }

            db.audit_logs.Add(new audit_logs
            {
                user_id = userId,
                user_email = userEmail,
                user_role = userRole,
                action = action,
                description = description,
                target_type = targetType,
                target_id = targetId,
                old_values = oldValues,
                new_values = newValues,
                ip_address = ip != null && ip.Length > 45 ? ip.Substring(0, 45) : ip,
                user_agent = ua,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            });
        }
    }
}
