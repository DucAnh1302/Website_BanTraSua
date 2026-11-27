using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using QL_WebBanTraSua.Filters;
using QL_WebBanTraSua.Helpers;
using QL_WebBanTraSua.Models;
using QL_WebBanTraSua.Services;

namespace QL_WebBanTraSua.Controllers
{
    [RoleAuthorize("pos", "staff", "admin")]
    public class PosController : Controller
    {
        private db_trasuaEntities db = new db_trasuaEntities();

        private const string PosCartSessionKey = "PosCart_Session";
        private const string PosVoucherIdSessionKey = "PosVoucherId_Session";
        private const string PosStateSessionKey = "PosState_Session";

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        private List<CartItem> GetPosCart()
        {
            var cart = Session[PosCartSessionKey] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session[PosCartSessionKey] = cart;
            }
            return cart;
        }

        private PosCheckoutState GetPosState()
        {
            var st = Session[PosStateSessionKey] as PosCheckoutState;
            if (st == null)
            {
                st = new PosCheckoutState();
                Session[PosStateSessionKey] = st;
            }
            return st;
        }

        private long GetCurrentUserId()
        {
            long uid = 0;
            if (Session["UserId"] != null)
            {
                long.TryParse(Session["UserId"].ToString(), out uid);
            }
            return uid;
        }

        private pos_shifts GetCurrentOpenShift()
        {
            long staffId = GetCurrentUserId();
            var ownShift = db.pos_shifts
                .Include(s => s.user)
                .Where(s => s.status == "open" && s.staff_id == staffId)
                .OrderByDescending(s => s.id)
                .FirstOrDefault();

            if (ownShift != null) return ownShift;

            return db.pos_shifts
                .Include(s => s.user)
                .Where(s => s.status == "open")
                .OrderByDescending(s => s.id)
                .FirstOrDefault();
        }

        // ========================================================
        // 1. MÀN HÌNH BÁN HÀNG POS CHÍNH (2 CỘT FULL-WIDTH, 0% JS)
        // ========================================================
        public ActionResult Index(string search, long? categoryId, long? chonMonId, string panel)
        {
            ViewBag.ActiveMenu = "BanHang";
            ViewBag.Title = "Quầy bán hàng POS";
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Panel = panel;

            ViewBag.Categories = db.categories.Where(c => c.is_active).OrderBy(c => c.name).ToList();

            var productQuery = db.products
                .Include(p => p.category)
                .Include(p => p.product_variants.Select(v => v.recipe_details.Select(r => r.ingredient)))
                .Where(p => p.is_active);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productQuery = productQuery.Where(p => p.category_id == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                productQuery = productQuery.Where(p => p.name.ToLower().Contains(s));
            }

            var products = productQuery.OrderBy(p => p.category_id).ThenBy(p => p.name).ToList();

            var toppings = db.toppings.Include(t => t.ingredient).Where(t => t.is_active).OrderBy(t => t.name).ToList();

            var outOfStockProducts = new HashSet<long>();
            var outOfStockVariants = new HashSet<long>();
            foreach (var p in products)
            {
                if (InventoryService.IsProductOutOfStock(p)) outOfStockProducts.Add(p.id);
                if (p.product_variants != null)
                {
                    foreach (var v in p.product_variants)
                    {
                        if (InventoryService.IsVariantOutOfStock(v)) outOfStockVariants.Add(v.id);
                    }
                }
            }

            var outOfStockToppings = new HashSet<long>();
            foreach (var t in toppings)
            {
                if (InventoryService.IsToppingOutOfStock(t)) outOfStockToppings.Add(t.id);
            }

            product selectedProduct = null;
            if (chonMonId.HasValue && chonMonId.Value > 0)
            {
                selectedProduct = db.products
                    .Include(p => p.product_variants)
                    .FirstOrDefault(p => p.id == chonMonId.Value && p.is_active);
            }

            var cart = GetPosCart();
            var state = GetPosState();
            decimal subtotal = cart.Sum(i => i.TotalPrice);
            decimal discount = 0m;
            voucher appliedVoucher = null;

            if (Session[PosVoucherIdSessionKey] != null)
            {
                long vid = Convert.ToInt64(Session[PosVoucherIdSessionKey]);
                var v = db.vouchers.Find(vid);
                if (v != null && cart.Count > 0)
                {
                    string vErr;
                    decimal calcDiscount;
                    var evaluated = InventoryService.EvaluateVoucher(db, v.code, subtotal, state.MatchedCustomerTier, out calcDiscount, out vErr);
                    if (evaluated != null)
                    {
                        appliedVoucher = evaluated;
                        discount = calcDiscount;
                    }
                    else
                    {
                        Session[PosVoucherIdSessionKey] = null;
                    }
                }
            }

            decimal finalTotal = Math.Max(0m, subtotal - discount);

            ViewBag.Products = products;
            ViewBag.Toppings = toppings;
            ViewBag.SelectedProduct = selectedProduct;
            ViewBag.OutOfStockProducts = outOfStockProducts;
            ViewBag.OutOfStockVariants = outOfStockVariants;
            ViewBag.OutOfStockToppings = outOfStockToppings;

            ViewBag.Cart = cart;
            ViewBag.PosState = state;
            ViewBag.Subtotal = subtotal;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = finalTotal;
            ViewBag.AppliedVoucher = appliedVoucher;

            ViewBag.CurrentShift = GetCurrentOpenShift();

            DateTime today = DateTime.Today;
            ViewBag.TodayOrders = db.orders
                .Include(o => o.user)
                .Where(o => o.order_type == "in_store" && o.created_at >= today)
                .OrderByDescending(o => o.id)
                .Take(20)
                .ToList();

            ViewBag.PendingOnlineCount = db.orders.Count(o => o.order_type == "online" && o.order_status == "pending");

            return View();
        }

        // ========================================================
        // 2. GIỎ HÀNG POS (THÊM NHANH / TÙY BIẾN / SỬA SL / XÓA)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemMonVaoDon(long variant_id, int quantity = 1, List<long> topping_ids = null, string sugar_level = "100", string ice_option = "normal", string note = "", string search = "", long? categoryId = null)
        {
            if (quantity <= 0) quantity = 1;

            var variant = db.product_variants
                .Include(v => v.product)
                .FirstOrDefault(v => v.id == variant_id);

            if (variant == null || variant.product == null || !variant.product.is_active)
            {
                TempData["Error"] = "Món nước không tồn tại hoặc đang ngừng phục vụ.";
                return RedirectToAction("Index", new { search = search, categoryId = categoryId });
            }

            var selectedToppings = new List<ToppingItem>();
            if (topping_ids != null && topping_ids.Count > 0)
            {
                var dbTops = db.toppings.Include(t => t.ingredient).Where(t => topping_ids.Contains(t.id) && t.is_active).ToList();
                var outOfStockTopNames = dbTops.Where(InventoryService.IsToppingOutOfStock).Select(t => t.name).ToList();
                if (outOfStockTopNames.Count > 0)
                {
                    TempData["Error"] = "Topping '" + string.Join(", ", outOfStockTopNames) + "' đã hết nguyên liệu kho.";
                    return RedirectToAction("Index", new { search = search, categoryId = categoryId, chonMonId = variant.product_id });
                }

                foreach (var top in dbTops)
                {
                    selectedToppings.Add(new ToppingItem { Id = top.id, Name = top.name, Price = top.price });
                }
            }

            string cleanSugar = string.IsNullOrWhiteSpace(sugar_level) ? "100" : sugar_level.Trim();
            string cleanIce = string.IsNullOrWhiteSpace(ice_option) ? "normal" : ice_option.Trim();
            string cleanNote = string.IsNullOrWhiteSpace(note) ? "" : note.Trim();
            string topKey = string.Join(",", selectedToppings.OrderBy(t => t.Id).Select(t => t.Id));

            var cart = GetPosCart();
            var existing = cart.FirstOrDefault(i =>
                i.VariantId == variant_id &&
                i.SugarLevel == cleanSugar &&
                i.IceOption == cleanIce &&
                (i.Note ?? "") == cleanNote &&
                string.Join(",", i.Toppings.OrderBy(t => t.Id).Select(t => t.Id)) == topKey);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = variant.product_id,
                    ProductName = variant.product.name,
                    Image = variant.product.image,
                    VariantId = variant.id,
                    Size = variant.size,
                    VariantPrice = variant.price,
                    SugarLevel = cleanSugar,
                    IceOption = cleanIce,
                    Toppings = selectedToppings,
                    Note = cleanNote,
                    Quantity = quantity
                });
            }

            Session[PosCartSessionKey] = cart;
            TempData["Success"] = "Đã thêm '" + variant.product.name + " (" + variant.size + ")' vào đơn POS.";
            return RedirectToAction("Index", new { search = search, categoryId = categoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatSoLuong(string lineId, int quantity)
        {
            var cart = GetPosCart();
            var item = cart.FirstOrDefault(i => i.CartItemId == lineId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
            Session[PosCartSessionKey] = cart;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaMon(string lineId)
        {
            var cart = GetPosCart();
            var item = cart.FirstOrDefault(i => i.CartItemId == lineId);
            if (item != null)
            {
                cart.Remove(item);
            }
            Session[PosCartSessionKey] = cart;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaGioHang()
        {
            Session[PosCartSessionKey] = new List<CartItem>();
            Session[PosVoucherIdSessionKey] = null;
            Session[PosStateSessionKey] = new PosCheckoutState();
            TempData["Success"] = "Đã làm trống đơn hiện tại.";
            return RedirectToAction("Index");
        }

        // ========================================================
        // 3. VOUCHER, TRA CỨU KHÁCH HÀNG & TÍNH TIỀN THỐI (0% JS)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApDungVoucher(string code)
        {
            var cart = GetPosCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Đơn POS đang trống, hãy thêm món trước khi áp dụng mã giảm giá.";
                return RedirectToAction("Index");
            }

            decimal subtotal = cart.Sum(i => i.TotalPrice);
            var state = GetPosState();

            string error;
            decimal discount;
            var voucher = InventoryService.EvaluateVoucher(db, code, subtotal, state.MatchedCustomerTier, out discount, out error);
            if (voucher == null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Index");
            }

            Session[PosVoucherIdSessionKey] = voucher.id;
            TempData["Success"] = string.Format("Áp dụng mã {0} thành công! Giảm {1:N0}đ.", voucher.code, discount);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GoVoucher()
        {
            Session[PosVoucherIdSessionKey] = null;
            TempData["Success"] = "Đã gỡ mã giảm giá khỏi đơn POS.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TraCuuKhachHang(string customer_phone, string customer_name, string customer_email)
        {
            var state = GetPosState();
            string cleanPhone = (customer_phone ?? "").Trim();
            state.CustomerPhone = cleanPhone;
            state.CustomerName = (customer_name ?? "").Trim();
            state.CustomerEmail = (customer_email ?? "").Trim();

            if (string.IsNullOrEmpty(cleanPhone))
            {
                state.MatchedCustomerId = null;
                state.MatchedCustomerTier = null;
                state.MatchedCustomerPoints = 0;
                state.IsNewCustomerWillCreate = false;
                Session[PosStateSessionKey] = state;
                TempData["Success"] = "Đã đặt khách vãng lai (không gắn SĐT).";
                return RedirectToAction("Index");
            }

            var existing = db.users.FirstOrDefault(u => u.phone == cleanPhone && u.role == "customer");
            if (existing != null)
            {
                state.MatchedCustomerId = existing.id;
                state.CustomerName = existing.name;
                state.CustomerEmail = existing.email;
                state.MatchedCustomerTier = existing.tier;
                state.MatchedCustomerPoints = existing.points;
                state.IsNewCustomerWillCreate = false;
                TempData["Success"] = "Tìm thấy hội viên: " + existing.name + " (Hạng " + existing.tier + " - " + existing.points + " điểm).";
            }
            else
            {
                state.MatchedCustomerId = null;
                state.MatchedCustomerTier = "Đồng";
                state.MatchedCustomerPoints = 0;
                state.IsNewCustomerWillCreate = true;
                TempData["Success"] = "SĐT " + cleanPhone + " chưa có tài khoản — hệ thống sẽ tự tạo khách thành viên mới khi chốt đơn.";
            }

            Session[PosStateSessionKey] = state;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TinhTienThoi(string payment_method, decimal? amount_given, string customer_phone, string customer_name, string customer_email)
        {
            var state = GetPosState();
            SyncStateInputs(state, payment_method, amount_given, customer_phone, customer_name, customer_email);

            var cart = GetPosCart();
            decimal subtotal = cart.Sum(i => i.TotalPrice);
            decimal discount = CalculateCurrentDiscount(subtotal, state.MatchedCustomerTier);
            decimal finalTotal = Math.Max(0m, subtotal - discount);

            if (state.PaymentMethod == "cash")
            {
                if (!state.AmountGiven.HasValue || state.AmountGiven.Value <= 0)
                {
                    TempData["Error"] = "Vui lòng nhập số tiền khách đưa để tính tiền thối.";
                }
                else if (state.AmountGiven.Value < finalTotal)
                {
                    TempData["Error"] = string.Format("Khách đưa thiếu {0:N0}đ so với tổng thanh toán {1:N0}đ.", finalTotal - state.AmountGiven.Value, finalTotal);
                }
                else
                {
                    TempData["Success"] = string.Format("Tiền khách đưa: {0:N0}đ | Tiền thối lại khách: {1:N0}đ.", state.AmountGiven.Value, state.AmountGiven.Value - finalTotal);
                }
            }
            else
            {
                TempData["Success"] = "Đã chọn thanh toán Chuyển khoản VietQR.";
            }

            Session[PosStateSessionKey] = state;
            return RedirectToAction("Index");
        }

        // ========================================================
        // 4. THANH TOÁN & TẠO ĐƠN TẠI QUẦY (TIỀN MẶT / VIETQR)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThanhToan(string payment_method, decimal? amount_given, string customer_phone, string customer_name, string customer_email)
        {
            var state = GetPosState();
            SyncStateInputs(state, payment_method, amount_given, customer_phone, customer_name, customer_email);
            Session[PosStateSessionKey] = state;

            var currentShift = GetCurrentOpenShift();
            if (currentShift == null)
            {
                TempData["Error"] = "Thu ngân chưa Mở ca làm việc két tiền! Vui lòng mở ca trước khi thanh toán đơn hàng.";
                return RedirectToAction("Index", new { panel = "moca" });
            }

            var cart = GetPosCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Chưa có sản phẩm nào trong đơn POS.";
                return RedirectToAction("Index");
            }

            string shortage = InventoryService.FindShortageForCart(db, cart);
            if (!string.IsNullOrEmpty(shortage))
            {
                TempData["Error"] = "Không đủ nguyên liệu pha chế trong kho: " + shortage;
                return RedirectToAction("Index");
            }

            user customer = ResolveCustomerForOrder(state);

            decimal subtotal = cart.Sum(i => i.TotalPrice);
            long? appliedVoucherId = Session[PosVoucherIdSessionKey] as long?;
            decimal discount = 0m;
            voucher voucherEntity = null;
            if (appliedVoucherId.HasValue)
            {
                var v = db.vouchers.Find(appliedVoucherId.Value);
                if (v != null)
                {
                    string vErr;
                    voucherEntity = InventoryService.EvaluateVoucher(db, v.code, subtotal, customer != null ? customer.tier : state.MatchedCustomerTier, out discount, out vErr);
                    if (voucherEntity == null)
                    {
                        TempData["Error"] = vErr;
                        return RedirectToAction("Index");
                    }
                }
            }

            decimal finalTotal = Math.Max(0m, subtotal - discount);
            string method = (state.PaymentMethod == "transfer") ? "transfer" : "cash";

            if (method == "cash")
            {
                if (!state.AmountGiven.HasValue || state.AmountGiven.Value < finalTotal)
                {
                    TempData["Error"] = string.Format("Số tiền khách đưa chưa đủ! Tổng đơn cần thanh toán là {0:N0}đ.", finalTotal);
                    return RedirectToAction("Index");
                }
            }

            long staffId = GetCurrentUserId();
            string staffEmail = Session["UserEmail"] as string;
            string staffRole = Session["UserRole"] as string;
            bool isCash = (method == "cash");

            order createdOrder = null;
            var objCtx = ((IObjectContextAdapter)db).ObjectContext;
            if (objCtx.Connection.State != ConnectionState.Open)
            {
                objCtx.Connection.Open();
            }

            using (var tx = objCtx.Connection.BeginTransaction())
            {
                try
                {
                    if (customer != null && customer.id == 0)
                    {
                        db.users.Add(customer);
                        db.SaveChanges();
                    }

                    createdOrder = new order
                    {
                        customer_id = customer != null ? (long?)customer.id : null,
                        staff_id = staffId > 0 ? (long?)staffId : null,
                        subtotal = subtotal,
                        shipping_fee = 0m,
                        discount_amount = discount,
                        final_total = finalTotal,
                        payment_method = method,
                        payment_status = isCash ? "paid" : "unpaid",
                        order_status = isCash ? "completed" : "pending",
                        order_type = "in_store",
                        shipping_name = customer != null ? customer.name : "Khách lẻ tại quầy",
                        shipping_phone = customer != null ? customer.phone : null,
                        shipping_address = "Bán tại quầy (POS)",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    db.orders.Add(createdOrder);
                    db.SaveChanges();

                    foreach (var line in cart)
                    {
                        var oi = new order_items
                        {
                            order_id = createdOrder.id,
                            variant_id = line.VariantId,
                            quantity = line.Quantity,
                            unit_price = line.VariantPrice,
                            sugar_level = line.SugarLevel,
                            ice_option = line.IceOption,
                            note = line.Note,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        db.order_items.Add(oi);
                        db.SaveChanges();

                        if (line.Toppings != null)
                        {
                            foreach (var top in line.Toppings)
                            {
                                db.order_item_toppings.Add(new order_item_toppings
                                {
                                    order_item_id = oi.id,
                                    topping_id = top.Id > 0 ? (long?)top.Id : null,
                                    topping_name = top.Name,
                                    price = top.Price
                                });
                            }
                        }
                    }
                    db.SaveChanges();

                    if (isCash)
                    {
                        InventoryService.DeductStockForOrder(db, createdOrder.id, allowShortage: false, notePrefix: "Trừ kho đơn POS #");

                        if (voucherEntity != null)
                        {
                            voucherEntity.used_count += 1;
                            voucherEntity.updated_at = DateTime.Now;
                        }

                        currentShift.cash_sales += finalTotal;
                        currentShift.expected_cash += finalTotal;
                        currentShift.updated_at = DateTime.Now;

                        if (customer != null)
                        {
                            InventoryService.AddLoyaltyPoints(customer, finalTotal);
                        }

                        InventoryService.WriteAuditLog(
                            db, Request, staffId, staffEmail, staffRole,
                            "POS_ORDER_CREATED",
                            string.Format("Thu ngân tạo đơn POS #{0} (Tiền mặt: {1:N0}đ) thuộc ca {2}", createdOrder.id, finalTotal, currentShift.shift_code),
                            "Order", createdOrder.id.ToString()
                        );
                        db.SaveChanges();
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["Error"] = "Không thể hoàn tất đơn POS: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }

            if (isCash)
            {
                decimal givenVal = state.AmountGiven ?? finalTotal;
                TempData["CashGiven"] = givenVal;
                TempData["CashChange"] = Math.Max(0m, givenVal - finalTotal);
                Session[PosCartSessionKey] = new List<CartItem>();
                Session[PosVoucherIdSessionKey] = null;
                Session[PosStateSessionKey] = new PosCheckoutState();
                TempData["Success"] = "Thanh toán tiền mặt đơn POS #" + createdOrder.id + " thành công!";
                return RedirectToAction("ThanhToanThanhCong", new { id = createdOrder.id });
            }
            else
            {
                if (voucherEntity != null)
                {
                    Session["PendingPosVoucher_" + createdOrder.id] = voucherEntity.id;
                }
                Session[PosCartSessionKey] = new List<CartItem>();
                Session[PosVoucherIdSessionKey] = null;
                Session[PosStateSessionKey] = new PosCheckoutState();
                return RedirectToAction("XacNhanChuyenKhoan", new { id = createdOrder.id });
            }
        }

        // Màn hình quét QR VietQR thuần thẻ <img> (không JS/Ajax)
        public ActionResult XacNhanChuyenKhoan(long id)
        {
            ViewBag.ActiveMenu = "BanHang";
            var ord = db.orders
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id && o.order_type == "in_store");

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn chuyển khoản #" + id;
                return RedirectToAction("Index");
            }

            if (ord.payment_status == "paid")
            {
                return RedirectToAction("ThanhToanThanhCong", new { id = ord.id });
            }

            string bankId = "MB";
            string accountNo = "0900000001";
            string accountName = "TEABLOOM SHOP";
            string memo = "TB" + ord.id;
            ViewBag.BankId = bankId;
            ViewBag.AccountNo = accountNo;
            ViewBag.AccountName = accountName;
            ViewBag.Memo = memo;
            ViewBag.QrUrl = string.Format(
                "https://img.vietqr.io/image/{0}-{1}-compact2.png?amount={2:0}&addInfo={3}&accountName={4}",
                bankId, accountNo, ord.final_total, Uri.EscapeDataString(memo), Uri.EscapeDataString(accountName));

            return View(ord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanChuyenKhoanPost(long id)
        {
            var currentShift = GetCurrentOpenShift();
            if (currentShift == null)
            {
                TempData["Error"] = "Không có ca làm việc nào đang mở để ghi nhận doanh thu chuyển khoản.";
                return RedirectToAction("XacNhanChuyenKhoan", new { id = id });
            }

            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.recipe_details.Select(rd => rd.ingredient)))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings.Select(oit => oit.topping.ingredient)))
                .FirstOrDefault(o => o.id == id && o.order_type == "in_store");

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn #" + id;
                return RedirectToAction("Index");
            }

            if (ord.payment_status == "paid")
            {
                return RedirectToAction("ThanhToanThanhCong", new { id = ord.id });
            }

            long staffId = GetCurrentUserId();
            string staffEmail = Session["UserEmail"] as string;
            string staffRole = Session["UserRole"] as string;

            var objCtx = ((IObjectContextAdapter)db).ObjectContext;
            if (objCtx.Connection.State != ConnectionState.Open) objCtx.Connection.Open();

            using (var tx = objCtx.Connection.BeginTransaction())
            {
                try
                {
                    InventoryService.DeductStockForOrder(db, ord.id, allowShortage: false, notePrefix: "Trừ kho đơn POS VietQR #");

                    ord.payment_status = "paid";
                    ord.order_status = "completed";
                    ord.updated_at = DateTime.Now;

                    object vKeyObj = Session["PendingPosVoucher_" + ord.id];
                    if (vKeyObj != null)
                    {
                        long vId = Convert.ToInt64(vKeyObj);
                        var vDb = db.vouchers.Find(vId);
                        if (vDb != null)
                        {
                            vDb.used_count += 1;
                            vDb.updated_at = DateTime.Now;
                        }
                        Session["PendingPosVoucher_" + ord.id] = null;
                    }

                    currentShift.transfer_sales += ord.final_total;
                    currentShift.updated_at = DateTime.Now;

                    if (ord.customer_id.HasValue && ord.user != null)
                    {
                        InventoryService.AddLoyaltyPoints(ord.user, ord.final_total);
                    }

                    InventoryService.WriteAuditLog(
                        db, Request, staffId, staffEmail, staffRole,
                        "POS_ORDER_VIETQR_CONFIRMED",
                        string.Format("Xác nhận đã thu VietQR đơn POS #{0} ({1:N0}đ) thuộc ca {2}", ord.id, ord.final_total, currentShift.shift_code),
                        "Order", ord.id.ToString()
                    );

                    db.SaveChanges();
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["Error"] = "Lỗi xác nhận thanh toán: " + ex.Message;
                    return RedirectToAction("XacNhanChuyenKhoan", new { id = id });
                }
            }

            TempData["Success"] = "Đã xác nhận nhận tiền chuyển khoản VietQR đơn #" + ord.id + "!";
            return RedirectToAction("ThanhToanThanhCong", new { id = ord.id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyGiaoDichCho(long id)
        {
            var ord = db.orders.Find(id);
            if (ord != null && ord.order_type == "in_store" && ord.payment_status == "unpaid" && ord.order_status == "pending")
            {
                ord.order_status = "cancelled";
                ord.updated_at = DateTime.Now;
                Session["PendingPosVoucher_" + ord.id] = null;
                db.SaveChanges();
                TempData["Success"] = "Đã hủy giao dịch chờ chuyển khoản #" + id + " (chưa trừ kho).";
            }
            return RedirectToAction("Index");
        }

        public ActionResult ThanhToanThanhCong(long id)
        {
            ViewBag.ActiveMenu = "BanHang";
            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("Index");
            }

            return View(ord);
        }

        public ActionResult InHoaDon(long id)
        {
            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn #" + id;
                return RedirectToAction("Index");
            }

            return View(ord);
        }

        public ActionResult XuatHoaDonPdf(long id)
        {
            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (ord == null) return HttpNotFound();

            byte[] pdfBytes = InvoicePdfHelper.BuildOrderInvoicePdf(ord);
            Response.AppendHeader("Content-Disposition", "inline; filename=HoaDon_POS_" + ord.id + ".pdf");
            return File(pdfBytes, "application/pdf");
        }

        // ========================================================
        // 5. QUẢN LÝ CA THU NGÂN & BÁO CÁO KẾT CA (Z-REPORT)
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MoCa(decimal opening_cash)
        {
            if (opening_cash < 0)
            {
                TempData["Error"] = "Tiền mặt đầu ca không được âm.";
                return RedirectToAction("Index", new { panel = "moca" });
            }

            var existingOpen = GetCurrentOpenShift();
            if (existingOpen != null)
            {
                TempData["Error"] = "Hiện đang có ca chưa kết thúc (" + existingOpen.shift_code + ").";
                return RedirectToAction("Index");
            }

            long staffId = GetCurrentUserId();
            string staffEmail = Session["UserEmail"] as string;
            string staffName = Session["UserName"] as string;
            string staffRole = Session["UserRole"] as string;

            DateTime today = DateTime.Today;
            int seq = db.pos_shifts.Count(s => s.opened_at >= today) + 1;
            string shiftCode = "SHIFT-" + DateTime.Now.ToString("yyyyMMdd") + "-" + seq.ToString("D2");
            while (db.pos_shifts.Any(s => s.shift_code == shiftCode))
            {
                seq++;
                shiftCode = "SHIFT-" + DateTime.Now.ToString("yyyyMMdd") + "-" + seq.ToString("D2");
            }

            var shift = new pos_shifts
            {
                shift_code = shiftCode,
                staff_id = staffId,
                opening_cash = opening_cash,
                cash_sales = 0m,
                transfer_sales = 0m,
                expected_cash = opening_cash,
                status = "open",
                opened_at = DateTime.Now,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };
            db.pos_shifts.Add(shift);
            db.SaveChanges();

            InventoryService.WriteAuditLog(
                db, Request, staffId, staffEmail, staffRole,
                "POS_SHIFT_OPENED",
                string.Format("Thu ngân [{0}] MỞ CA {1} - Tiền đầu ca: {2:N0}đ", staffName, shiftCode, opening_cash),
                "PosShift", shift.id.ToString()
            );
            db.SaveChanges();

            TempData["Success"] = string.Format("Đã mở ca {0} thành công! Tiền bàn giao đầu ca: {1:N0}đ.", shiftCode, opening_cash);
            return RedirectToAction("Index");
        }

        public ActionResult KetCa(long? shiftId)
        {
            ViewBag.ActiveMenu = "KetCa";
            ViewBag.Title = "Kết ca & Báo cáo Z-Report";
            ViewBag.CurrentShift = GetCurrentOpenShift();

            pos_shifts targetShift = null;
            if (shiftId.HasValue)
            {
                targetShift = db.pos_shifts.Include(s => s.user).FirstOrDefault(s => s.id == shiftId.Value);
            }
            if (targetShift == null)
            {
                targetShift = GetCurrentOpenShift();
            }

            ViewBag.RecentClosedShifts = db.pos_shifts
                .Include(s => s.user)
                .Where(s => s.status == "closed")
                .OrderByDescending(s => s.id)
                .Take(10)
                .ToList();

            return View(targetShift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KetCa(long shift_id, decimal actual_cash, string notes)
        {
            var shift = db.pos_shifts.Include(s => s.user).FirstOrDefault(s => s.id == shift_id && s.status == "open");
            if (shift == null)
            {
                TempData["Error"] = "Ca làm việc không tồn tại hoặc đã được kết thúc trước đó.";
                return RedirectToAction("KetCa");
            }

            if (actual_cash < 0)
            {
                TempData["Error"] = "Số tiền thực tế kiểm đếm không được âm.";
                return RedirectToAction("KetCa", new { shiftId = shift_id });
            }

            decimal diff = actual_cash - shift.expected_cash;
            shift.actual_cash = actual_cash;
            shift.difference = diff;
            shift.notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            shift.status = "closed";
            shift.closed_at = DateTime.Now;
            shift.updated_at = DateTime.Now;

            string diffText = diff == 0m
                ? "Khớp 100%"
                : (diff > 0m ? string.Format("Thừa +{0:N0}đ", diff) : string.Format("Thiếu -{0:N0}đ", Math.Abs(diff)));

            long staffId = GetCurrentUserId();
            InventoryService.WriteAuditLog(
                db, Request, staffId, Session["UserEmail"] as string, Session["UserRole"] as string,
                "POS_SHIFT_CLOSED",
                string.Format("Thu ngân [{0}] KẾT CA {1}. Thực tế: {2:N0}đ ({3})", Session["UserName"], shift.shift_code, actual_cash, diffText),
                "PosShift", shift.id.ToString(),
                string.Format("{{\"expected_cash\":{0}}}", shift.expected_cash),
                string.Format("{{\"actual_cash\":{0},\"difference\":{1}}}", actual_cash, diff)
            );
            db.SaveChanges();

            TempData["Success"] = string.Format("Đã kết ca {0}! Kết quả kiểm kê két: {1}.", shift.shift_code, diffText);
            return RedirectToAction("KetCa", new { shiftId = shift.id });
        }

        public ActionResult LichSuCa()
        {
            ViewBag.ActiveMenu = "KetCa";
            ViewBag.Title = "Lịch sử các ca làm việc";
            ViewBag.CurrentShift = GetCurrentOpenShift();
            var shifts = db.pos_shifts.Include(s => s.user).OrderByDescending(s => s.id).Take(50).ToList();
            return View(shifts);
        }

        // ========================================================
        // 6. CHỐNG GIAN LẬN: MANAGER OVERRIDE DUYỆT HỦY ĐƠN TẠI QUẦY
        // ========================================================
        public ActionResult HuyDon(long id)
        {
            ViewBag.ActiveMenu = "BanHang";
            ViewBag.Title = "Quản lý duyệt hủy đơn #" + id;
            ViewBag.CurrentShift = GetCurrentOpenShift();

            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("Index");
            }

            return View(ord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDon(long id, string manager_email, string manager_password, string cancel_reason)
        {
            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items)
                .FirstOrDefault(o => o.id == id);

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("Index");
            }

            if (ord.order_status == "cancelled")
            {
                TempData["Error"] = "Đơn hàng #" + id + " đã bị hủy trước đó.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(cancel_reason))
            {
                TempData["Error"] = "Vui lòng nhập lý do hủy đơn.";
                return RedirectToAction("HuyDon", new { id = id });
            }

            string cleanEmail = (manager_email ?? "").Trim().ToLower();
            var manager = db.users.FirstOrDefault(u => u.email.ToLower() == cleanEmail);

            long staffId = GetCurrentUserId();
            string staffEmail = Session["UserEmail"] as string;
            string staffRole = Session["UserRole"] as string;

            if (manager == null || !manager.status || !string.Equals(manager.role, "admin", StringComparison.OrdinalIgnoreCase) || manager.password != manager_password)
            {
                InventoryService.WriteAuditLog(
                    db, Request, staffId, staffEmail, staffRole,
                    "POS_CANCEL_UNAUTHORIZED_ATTEMPT",
                    "CẢNH BÁO: Hủy đơn POS #" + ord.id + " thất bại do xác thực Quản lý không hợp lệ (" + cleanEmail + ")",
                    "Order", ord.id.ToString()
                );
                db.SaveChanges();

                TempData["Error"] = "Xác thực Quản lý thất bại! Chỉ tài khoản Quản trị viên (Admin) hợp lệ mới được duyệt hủy đơn đã thu tiền.";
                return RedirectToAction("HuyDon", new { id = id });
            }

            string oldStatus = ord.order_status;
            ord.order_status = "cancelled";
            ord.updated_at = DateTime.Now;

            InventoryService.RefundOrderStock(db, ord, "Hoàn kho do Quản lý [" + manager.name + "] duyệt hủy đơn POS #" + ord.id + ": " + cancel_reason.Trim());

            if (oldStatus == "completed" && ord.customer_id.HasValue && ord.user != null)
            {
                InventoryService.SubtractLoyaltyPoints(ord.user, ord.final_total);
            }

            var currentShift = GetCurrentOpenShift();
            if (currentShift != null && ord.created_at.HasValue && ord.created_at.Value >= currentShift.opened_at && ord.payment_status == "paid")
            {
                if (ord.payment_method == "cash")
                {
                    currentShift.cash_sales = Math.Max(0m, currentShift.cash_sales - ord.final_total);
                    currentShift.expected_cash = Math.Max(0m, currentShift.expected_cash - ord.final_total);
                }
                else if (ord.payment_method == "transfer")
                {
                    currentShift.transfer_sales = Math.Max(0m, currentShift.transfer_sales - ord.final_total);
                }
                currentShift.updated_at = DateTime.Now;
            }

            InventoryService.WriteAuditLog(
                db, Request, staffId, staffEmail, staffRole,
                "POS_ORDER_CANCEL_MANAGER_APPROVED",
                "Quản lý [" + manager.name + "] duyệt hủy đơn POS #" + ord.id + ". Lý do: " + cancel_reason.Trim(),
                "Order", ord.id.ToString(),
                "{\"order_status\":\"" + oldStatus + "\"}",
                "{\"order_status\":\"cancelled\",\"approved_by\":\"" + manager.email + "\"}"
            );
            db.SaveChanges();

            TempData["Success"] = "Quản lý " + manager.name + " đã duyệt hủy Đơn #" + ord.id + "! Nguyên liệu đã được hoàn trả lại kho.";
            return RedirectToAction("Index");
        }

        // ========================================================
        // 7. TIẾP NHẬN & XỬ LÝ ĐƠN ĐẶT HÀNG ONLINE (UC03 CHO POS)
        // ========================================================
        public ActionResult DonOnline(string status, string search)
        {
            ViewBag.ActiveMenu = "DonOnline";
            ViewBag.Title = "Tiếp nhận Đơn Online";
            ViewBag.CurrentShift = GetCurrentOpenShift();
            ViewBag.Status = status;
            ViewBag.Search = search;

            var query = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items)
                .Where(o => o.order_type == "online");

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.order_status == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                long oid;
                if (long.TryParse(s.TrimStart('#'), out oid))
                {
                    query = query.Where(o => o.id == oid || (o.shipping_phone != null && o.shipping_phone.Contains(s)));
                }
                else
                {
                    query = query.Where(o =>
                        (o.shipping_name != null && o.shipping_name.ToLower().Contains(s)) ||
                        (o.shipping_phone != null && o.shipping_phone.Contains(s)));
                }
            }

            var orders = query.OrderByDescending(o => o.id).Take(60).ToList();
            return View(orders);
        }

        public ActionResult ChiTietDonOnline(long id)
        {
            ViewBag.ActiveMenu = "DonOnline";
            ViewBag.Title = "Chi tiết Đơn Online #" + id;
            ViewBag.CurrentShift = GetCurrentOpenShift();

            var ord = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("DonOnline");
            }

            return View(ord);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatTrangThaiOnline(long id, string order_status, string payment_status)
        {
            var ord = db.orders
                .Include(o => o.user)
                .FirstOrDefault(o => o.id == id);

            if (ord == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("DonOnline");
            }

            string oldOrderStatus = ord.order_status;
            string oldPayStatus = ord.payment_status;

            ord.order_status = order_status;
            if (!string.IsNullOrWhiteSpace(payment_status))
            {
                ord.payment_status = payment_status;
            }
            ord.updated_at = DateTime.Now;

            if (oldOrderStatus != "cancelled" && order_status == "cancelled")
            {
                InventoryService.RefundOrderStock(db, ord, "Hoàn kho từ hủy đơn Online #" + ord.id + " (POS xử lý)");
                if (oldOrderStatus == "completed" && ord.customer_id.HasValue && ord.user != null)
                {
                    InventoryService.SubtractLoyaltyPoints(ord.user, ord.final_total);
                }
            }
            else if (oldOrderStatus != "completed" && order_status == "completed" && ord.customer_id.HasValue && ord.user != null)
            {
                InventoryService.AddLoyaltyPoints(ord.user, ord.final_total);
            }

            InventoryService.WriteAuditLog(
                db, Request, GetCurrentUserId(), Session["UserEmail"] as string, Session["UserRole"] as string,
                "POS_UPDATE_ONLINE_ORDER",
                string.Format("POS cập nhật đơn Online #{0}: {1} -> {2} (TT: {3} -> {4})", ord.id, oldOrderStatus, order_status, oldPayStatus, ord.payment_status),
                "Order", ord.id.ToString()
            );
            db.SaveChanges();

            TempData["Success"] = "Đã cập nhật đơn hàng #" + ord.id + " thành công!";
            return RedirectToAction("ChiTietDonOnline", new { id = id });
        }

        // ---------- Private Helpers ----------
        private void SyncStateInputs(PosCheckoutState state, string paymentMethod, decimal? amountGiven, string phone, string name, string email)
        {
            if (!string.IsNullOrWhiteSpace(paymentMethod)) state.PaymentMethod = paymentMethod;
            if (amount_given_provided(amountGiven)) state.AmountGiven = amountGiven;
            else if (amountGiven == null && paymentMethod == "cash") state.AmountGiven = null;

            if (phone != null) state.CustomerPhone = phone.Trim();
            if (name != null) state.CustomerName = name.Trim();
            if (email != null) state.CustomerEmail = email.Trim();
        }

        private bool amount_given_provided(decimal? val) { return val.HasValue; }

        private decimal CalculateCurrentDiscount(decimal subtotal, string customerTier)
        {
            if (Session[PosVoucherIdSessionKey] == null) return 0m;
            long vid = Convert.ToInt64(Session[PosVoucherIdSessionKey]);
            var v = db.vouchers.Find(vid);
            if (v == null) return 0m;
            decimal d;
            string err;
            var res = InventoryService.EvaluateVoucher(db, v.code, subtotal, customerTier, out d, out err);
            return res != null ? d : 0m;
        }

        private user ResolveCustomerForOrder(PosCheckoutState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.CustomerPhone))
            {
                return null;
            }

            string cleanPhone = state.CustomerPhone.Trim();
            var existing = db.users.FirstOrDefault(u => u.phone == cleanPhone && u.role == "customer");
            if (existing != null)
            {
                return existing;
            }

            string cleanName = string.IsNullOrWhiteSpace(state.CustomerName) ? ("Khách " + cleanPhone) : state.CustomerName.Trim();
            string cleanEmail = string.IsNullOrWhiteSpace(state.CustomerEmail) ? (cleanPhone + "@trasua.com") : state.CustomerEmail.Trim();

            if (db.users.Any(u => u.email.ToLower() == cleanEmail.ToLower()))
            {
                cleanEmail = cleanPhone + "_" + DateTime.Now.Ticks.ToString().Substring(10) + "@trasua.com";
            }

            return new user
            {
                name = cleanName,
                phone = cleanPhone,
                email = cleanEmail,
                password = cleanPhone,
                role = "customer",
                status = true,
                points = 0,
                tier = "Đồng",
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };
        }
    }
}
