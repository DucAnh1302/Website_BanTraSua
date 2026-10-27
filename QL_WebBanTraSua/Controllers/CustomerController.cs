using QL_WebBanTraSua.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace QL_WebBanTraSua.Controllers
{
    public class CustomerController : Controller
    {
        private db_trasuaEntities data = new db_trasuaEntities();

        private const string CartSessionKey = "GioHang_Session";
        private const string VoucherSessionKey = "Voucher_Session";
        private const string DiscountSessionKey = "GiamGia_Session";

        // ==========================================
        // 0. TRANG CHỦ (HOME PAGE / INDEX)
        // ==========================================
        public ActionResult Index()
        {
            ViewBag.Categories = data.categories.Where(c => c.is_active).ToList();
            var bestSellers = data.products.Where(p => p.is_active).Take(6).ToList();
            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View(bestSellers);
        }

        // ==========================================
        // 1. XEM DANH SÁCH SẢN PHẨM (MENU / PRODUCTS CATALOG) - NGUYỄN HOÀNG BẢO
        // ==========================================
        public ActionResult Products(long? categoryId, string search, string sort = "newest", string priceRange = "all")
        {
            var query = data.products.Where(p => p.is_active);

            // 1. Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.category_id == categoryId.Value);
                ViewBag.CurrentCategory = categoryId.Value;
            }

            var list = query.ToList();

            // 2. Lọc theo từ khóa tìm kiếm (hỗ trợ cả tiếng Việt có dấu và không dấu, tìm theo cả tên và mô tả)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string searchTrim = search.Trim();
                string searchLower = searchTrim.ToLower();
                string searchNoTone = RemoveDiacritics(searchTrim);

                list = list.Where(p =>
                {
                    string pName = p.name ?? "";
                    string pDesc = p.description ?? "";

                    bool matchName = pName.ToLower().Contains(searchLower) || RemoveDiacritics(pName).Contains(searchNoTone);
                    bool matchDesc = pDesc.ToLower().Contains(searchLower) || RemoveDiacritics(pDesc).Contains(searchNoTone);

                    return matchName || matchDesc;
                }).ToList();

                ViewBag.SearchKeyword = searchTrim;
            }

            // 3. Lọc theo mức giá (tính theo giá khởi điểm thấp nhất của món)
            if (!string.IsNullOrEmpty(priceRange) && priceRange != "all")
            {
                if (priceRange == "under28" || priceRange == "under30" || priceRange == "under40")
                {
                    int maxVal = (priceRange == "under28") ? 28000 : ((priceRange == "under30") ? 30000 : 40000);
                    list = list.Where(p => p.product_variants.Any() && p.product_variants.Min(v => v.price) <= maxVal).ToList();
                }
                else if (priceRange == "28to32" || priceRange == "30to35" || priceRange == "40to60")
                {
                    if (priceRange == "28to32")
                    {
                        list = list.Where(p => p.product_variants.Any() && p.product_variants.Min(v => v.price) > 28000 && p.product_variants.Min(v => v.price) <= 32000).ToList();
                    }
                    else if (priceRange == "30to35")
                    {
                        list = list.Where(p => p.product_variants.Any() && p.product_variants.Min(v => v.price) >= 30000 && p.product_variants.Min(v => v.price) <= 35000).ToList();
                    }
                    else
                    {
                        list = list.Where(p => p.product_variants.Any() && p.product_variants.Min(v => v.price) >= 40000 && p.product_variants.Min(v => v.price) <= 60000).ToList();
                    }
                }
                else if (priceRange == "above32" || priceRange == "above35" || priceRange == "above60")
                {
                    int minVal = (priceRange == "above32") ? 32000 : ((priceRange == "above35") ? 35000 : 60000);
                    list = list.Where(p => p.product_variants.Any() && p.product_variants.Min(v => v.price) > minVal).ToList();
                }

                ViewBag.CurrentPriceRange = priceRange;
            }

            // 4. Sắp xếp
            if (sort == "price-asc")
            {
                // Giá: Thấp đến Cao
                list = list.OrderBy(p => p.product_variants.Any() ? p.product_variants.Min(v => v.price) : 0).ToList();
            }
            else if (sort == "price-desc")
            {
                // Giá: Cao đến Thấp
                list = list.OrderByDescending(p => p.product_variants.Any() ? p.product_variants.Min(v => v.price) : 0).ToList();
            }
            else if (sort == "name-asc")
            {
                // Tên: A - Z
                list = list.OrderBy(p => p.name).ToList();
            }
            else
            {
                // Mới nhất (Mặc định)
                list = list.OrderByDescending(p => p.id).ToList();
            }

            ViewBag.CurrentSort = sort;
            ViewBag.Categories = data.categories.Where(c => c.is_active).ToList();
            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);

            return View(list);
        }

        // Hàm loại bỏ dấu tiếng Việt để tìm kiếm thông minh không dấu
        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLower().Replace("đ", "d").Replace("Đ", "d");
        }

        // ==========================================
        // 2. XEM CHI TIẾT SẢN PHẨM (PRODUCT DETAILS) - NGUYỄN HOÀNG BẢO
        // ==========================================
        public ActionResult Details(long id)
        {
            var product = data.products.FirstOrDefault(p => p.id == id && p.is_active);
            if (product == null)
            {
                return HttpNotFound();
            }

            ViewBag.Variants = product.product_variants.OrderBy(v => v.price).ToList();
            ViewBag.Toppings = data.toppings.Where(t => t.is_active).ToList();
            ViewBag.RelatedProducts = data.products
                .Where(p => p.category_id == product.category_id && p.id != product.id && p.is_active)
                .Take(4)
                .ToList();

            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View(product);
        }

        // ==========================================
        // 3. QUẢN LÝ GIỎ HÀNG (SHOPPING CART) - NGUYỄN HOÀNG BẢO
        // ==========================================
        public List<CartItem> GetCart()
        {
            var cart = Session[CartSessionKey] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session[CartSessionKey] = cart;
            }
            return cart;
        }

        [HttpPost]
        public ActionResult ThemGioHang(long variantId, string sugar = "100", string ice = "normal", List<long> toppingIds = null, string note = "", int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var variant = data.product_variants.FirstOrDefault(v => v.id == variantId);
            if (variant == null || variant.product == null || !variant.product.is_active)
            {
                TempData["ErrorMessage"] = "Sản phẩm hiện không khả dụng.";
                return RedirectToAction("Products");
            }

            var product = variant.product;
            var cart = GetCart();

            var selectedToppings = new List<ToppingItem>();
            if (toppingIds != null && toppingIds.Count > 0)
            {
                var dbToppings = data.toppings.Where(t => toppingIds.Contains(t.id) && t.is_active).ToList();
                foreach (var top in dbToppings)
                {
                    selectedToppings.Add(new ToppingItem
                    {
                        Id = top.id,
                        Name = top.name,
                        Price = top.price
                    });
                }
            }

            string toppingsKey = string.Join(",", selectedToppings.OrderBy(t => t.Id).Select(t => t.Id));

            var existingItem = cart.FirstOrDefault(item =>
                item.VariantId == variantId &&
                item.SugarLevel == sugar &&
                item.IceOption == ice &&
                string.Join(",", item.Toppings.OrderBy(t => t.Id).Select(t => t.Id)) == toppingsKey);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = product.id,
                    ProductName = product.name,
                    Image = product.image,
                    VariantId = variant.id,
                    Size = variant.size,
                    VariantPrice = variant.price,
                    SugarLevel = sugar,
                    IceOption = ice,
                    Toppings = selectedToppings,
                    Note = note,
                    Quantity = quantity
                };
                cart.Add(newItem);
            }

            Session[CartSessionKey] = cart;
            return RedirectToAction("Cart");
        }

        public ActionResult Cart()
        {
            var cart = GetCart();
            ViewBag.CartCount = cart.Sum(item => item.Quantity);

            decimal subtotal = cart.Sum(item => item.TotalPrice);
            decimal discount = (decimal)(Session[DiscountSessionKey] ?? 0m);
            decimal shipping = cart.Count > 0 ? 15000m : 0m;
            decimal total = subtotal + shipping - discount;
            if (total < 0) total = 0;

            ViewBag.Subtotal = subtotal;
            ViewBag.ShippingFee = shipping;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = total;
            ViewBag.AppliedVoucher = Session[VoucherSessionKey] as voucher;

            return View(cart);
        }

        [HttpPost]
        public ActionResult CapNhatGioHang(string cartItemId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.CartItemId == cartItemId);
            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Remove(item);
                }
            }
            Session[CartSessionKey] = cart;
            return RedirectToAction("Cart");
        }

        public ActionResult XoaGioHang(string cartItemId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.CartItemId == cartItemId);
            if (item != null)
            {
                cart.Remove(item);
            }
            Session[CartSessionKey] = cart;
            return RedirectToAction("Cart");
        }

        public ActionResult XoaTatCa()
        {
            Session[CartSessionKey] = new List<CartItem>();
            Session[VoucherSessionKey] = null;
            Session[DiscountSessionKey] = 0m;
            return RedirectToAction("Cart");
        }

        // ==========================================
        // 4. THANH TOÁN & ĐẶT HÀNG (CHECKOUT) - NGUYỄN HOÀNG BẢO
        // ==========================================
        public ActionResult Checkout()
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                return RedirectToAction("Products");
            }

            decimal subtotal = cart.Sum(item => item.TotalPrice);
            decimal discount = (decimal)(Session[DiscountSessionKey] ?? 0m);
            decimal shipping = 15000m;
            decimal total = subtotal + shipping - discount;
            if (total < 0) total = 0;

            ViewBag.Subtotal = subtotal;
            ViewBag.ShippingFee = shipping;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = total;
            ViewBag.AppliedVoucher = Session[VoucherSessionKey] as voucher;

            return View(cart);
        }

        [HttpPost]
        public ActionResult ApDungVoucher(string voucherCode, string shipping_name = "", string shipping_phone = "", string shipping_email = "", string shipping_address = "", string checkout_note = "")
        {
            // Lưu lại thông tin nhận hàng khách đã gõ để không bị mất khi trang tải lại
            if (!string.IsNullOrWhiteSpace(shipping_name)) Session["CustomerName"] = shipping_name.Trim();
            if (!string.IsNullOrWhiteSpace(shipping_phone)) Session["CustomerPhone"] = shipping_phone.Trim();
            if (!string.IsNullOrWhiteSpace(shipping_email)) Session["CustomerEmail"] = shipping_email.Trim();
            if (!string.IsNullOrWhiteSpace(shipping_address)) Session["CustomerAddress"] = shipping_address.Trim();
            if (!string.IsNullOrWhiteSpace(checkout_note)) Session["CustomerNote"] = checkout_note.Trim();

            var cart = GetCart();
            decimal subtotal = cart.Sum(i => i.TotalPrice);

            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                TempData["VoucherError"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction("Checkout");
            }

            string code = voucherCode.Trim();
            var v = data.vouchers.FirstOrDefault(x => x.code.ToLower() == code.ToLower() && x.is_active);

            if (v == null)
            {
                TempData["VoucherError"] = "Mã giảm giá không tồn tại hoặc đã hết hiệu lực.";
                return RedirectToAction("Checkout");
            }

            if (v.expires_at.HasValue && v.expires_at.Value < DateTime.Now)
            {
                TempData["VoucherError"] = "Mã giảm giá đã hết hạn sử dụng.";
                return RedirectToAction("Checkout");
            }

            if (v.usage_limit.HasValue && v.used_count >= v.usage_limit.Value)
            {
                TempData["VoucherError"] = "Mã giảm giá đã hết lượt sử dụng.";
                return RedirectToAction("Checkout");
            }

            if (subtotal < v.min_order_value)
            {
                TempData["VoucherError"] = $"Đơn hàng tối thiểu phải từ {v.min_order_value:N0}đ để áp dụng mã này.";
                return RedirectToAction("Checkout");
            }

            decimal discount = 0;
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

            Session[VoucherSessionKey] = v;
            Session[DiscountSessionKey] = discount;
            TempData["VoucherSuccess"] = $"Áp dụng mã thành công! Bạn được giảm {discount:N0}đ.";

            return RedirectToAction("Checkout");
        }

        public ActionResult HuyVoucher()
        {
            Session[VoucherSessionKey] = null;
            Session[DiscountSessionKey] = 0m;
            TempData["VoucherSuccess"] = "Đã hủy áp dụng mã giảm giá.";
            return RedirectToAction("Checkout");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatHang(FormCollection form)
        {
            var cart = GetCart();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Products");
            }

            string shippingName = form["shipping_name"];
            string shippingPhone = form["shipping_phone"];
            string shippingEmail = form["shipping_email"];
            string shippingAddress = form["shipping_address"];
            string paymentMethod = form["payment_method"] ?? "cash";
            string checkoutNote = form["checkout_note"];

            if (string.IsNullOrWhiteSpace(shippingName) || string.IsNullOrWhiteSpace(shippingPhone) || string.IsNullOrWhiteSpace(shippingAddress))
            {
                TempData["CheckoutError"] = "Vui lòng nhập đầy đủ thông tin người nhận (Họ tên, SĐT, Địa chỉ).";
                return RedirectToAction("Checkout");
            }

            decimal subtotal = cart.Sum(item => item.TotalPrice);
            decimal discount = (decimal)(Session[DiscountSessionKey] ?? 0m);
            decimal shipping = 15000m;
            decimal finalTotal = subtotal + shipping - discount;
            if (finalTotal < 0) finalTotal = 0;

            var voucher = Session[VoucherSessionKey] as voucher;

            var newOrder = new order
            {
                customer_id = null,
                staff_id = null,
                voucher_id = voucher != null ? (long?)voucher.id : null,
                subtotal = subtotal,
                shipping_fee = shipping,
                discount_amount = discount,
                final_total = finalTotal,
                payment_method = paymentMethod,
                payment_status = "unpaid",
                order_status = "pending",
                order_type = "online",
                shipping_name = shippingName.Trim(),
                shipping_phone = shippingPhone.Trim(),
                shipping_address = shippingAddress.Trim(),
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            data.orders.Add(newOrder);
            data.SaveChanges();

            foreach (var item in cart)
            {
                var orderItem = new order_items
                {
                    order_id = newOrder.id,
                    variant_id = item.VariantId,
                    quantity = item.Quantity,
                    unit_price = item.VariantPrice,
                    sugar_level = item.SugarLevel,
                    ice_option = item.IceOption,
                    note = item.Note,
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now
                };

                data.order_items.Add(orderItem);
                data.SaveChanges();

                if (item.Toppings != null && item.Toppings.Count > 0)
                {
                    foreach (var top in item.Toppings)
                    {
                        var itemTopping = new order_item_toppings
                        {
                            order_item_id = orderItem.id,
                            topping_id = top.Id > 0 ? (long?)top.Id : null,
                            topping_name = top.Name,
                            price = top.Price
                        };
                        data.order_item_toppings.Add(itemTopping);
                    }
                }
            }

            if (voucher != null)
            {
                var vDb = data.vouchers.FirstOrDefault(x => x.id == voucher.id);
                if (vDb != null)
                {
                    vDb.used_count += 1;
                    vDb.updated_at = DateTime.Now;
                }
            }

            data.SaveChanges();

            Session["CustomerName"] = shippingName.Trim();
            Session["CustomerPhone"] = shippingPhone.Trim();
            if (!string.IsNullOrWhiteSpace(shippingEmail)) Session["CustomerEmail"] = shippingEmail.Trim();
            Session["CustomerAddress"] = shippingAddress.Trim();
            Session["CustomerNote"] = null;
            Session["LastOrderId"] = newOrder.id;
            Session[CartSessionKey] = new List<CartItem>();
            Session[VoucherSessionKey] = null;
            Session[DiscountSessionKey] = 0m;

            return RedirectToAction("OrderSuccess", new { id = newOrder.id });
        }

        public ActionResult OrderSuccess(long id)
        {
            var order = data.orders.FirstOrDefault(o => o.id == id);
            if (order == null)
            {
                return RedirectToAction("Products");
            }
            return View(order);
        }

        // ==========================================
        // 5. QUẢN LÝ HÓA ĐƠN & LỊCH SỬ ĐƠN HÀNG (INVOICE / PROFILE) - NGUYỄN HOÀNG BẢO
        // ==========================================
        public ActionResult OrderHistory(string phone)
        {
            string keyword = phone;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                keyword = Session["CustomerPhone"] as string;
            }

            List<order> orders = new List<order>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string clean = keyword.Trim().TrimStart('#');
                long orderId;
                bool isOrderId = long.TryParse(clean, out orderId);

                orders = data.orders
                    .Where(o => o.shipping_phone == keyword.Trim() || (isOrderId && o.id == orderId))
                    .OrderByDescending(o => o.created_at)
                    .ToList();
                ViewBag.Phone = keyword;
            }
            else
            {
                orders = data.orders
                    .Where(o => o.order_type == "online")
                    .OrderByDescending(o => o.created_at)
                    .Take(10)
                    .ToList();
            }

            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View(orders);
        }

        public ActionResult OrderDetail(long id)
        {
            var order = data.orders.FirstOrDefault(o => o.id == id);
            if (order == null)
            {
                return HttpNotFound();
            }
            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View(order);
        }

        // ==========================================
        // 6. XUẤT HÓA ĐƠN FILE PDF (PDF EXPORT) - NGUYỄN HOÀNG BẢO
        // ==========================================
        public ActionResult XuatHoaDonPDF(long id)
        {
            var order = data.orders.FirstOrDefault(o => o.id == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document document = new Document(PageSize.A4, 36f, 36f, 36f, 36f);
                PdfWriter writer = PdfWriter.GetInstance(document, ms);
                document.Open();

                BaseFont bf;
                try
                {
                    string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
                    if (!System.IO.File.Exists(fontPath))
                    {
                        fontPath = @"C:\Windows\Fonts\arial.ttf";
                    }
                    bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                }
                catch
                {
                    bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.WINANSI, BaseFont.NOT_EMBEDDED);
                }

                Font titleFont = new Font(bf, 20, Font.BOLD, new BaseColor(24, 24, 27));
                Font subTitleFont = new Font(bf, 12, Font.ITALIC, new BaseColor(113, 113, 122));
                Font sectionTitleFont = new Font(bf, 13, Font.BOLD, new BaseColor(225, 29, 72));
                Font headerTableFont = new Font(bf, 10, Font.BOLD, BaseColor.WHITE);
                Font boldFont = new Font(bf, 10, Font.BOLD, new BaseColor(39, 39, 42));
                Font normalFont = new Font(bf, 10, Font.NORMAL, new BaseColor(63, 63, 70));
                Font smallFont = new Font(bf, 9, Font.NORMAL, new BaseColor(113, 113, 122));

                Paragraph title = new Paragraph("TIỆM TRÀ SỮA", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);

                Paragraph slogan = new Paragraph("Đậm vị trà, Thơm vị sữa - 140 Lê Trọng Tấn, Q. Tân Phú, TP. HCM", subTitleFont);
                slogan.Alignment = Element.ALIGN_CENTER;
                slogan.SpacingAfter = 15f;
                document.Add(slogan);

                Paragraph invoiceHeader = new Paragraph($"HÓA ĐƠN BÁN HÀNG #{order.id}", sectionTitleFont);
                invoiceHeader.Alignment = Element.ALIGN_CENTER;
                invoiceHeader.SpacingAfter = 20f;
                document.Add(invoiceHeader);

                PdfPTable infoTable = new PdfPTable(2);
                infoTable.WidthPercentage = 100;
                infoTable.SetWidths(new float[] { 50f, 50f });
                infoTable.SpacingAfter = 15f;

                PdfPCell c1 = new PdfPCell();
                c1.Border = Rectangle.NO_BORDER;
                c1.AddElement(new Paragraph($"Khách hàng: {order.shipping_name ?? "Khách hàng"}", normalFont));
                c1.AddElement(new Paragraph($"Số điện thoại: {order.shipping_phone ?? "N/A"}", normalFont));
                c1.AddElement(new Paragraph($"Địa chỉ nhận hàng: {order.shipping_address ?? "Tại quán"}", normalFont));
                infoTable.AddCell(c1);

                PdfPCell c2 = new PdfPCell();
                c2.Border = Rectangle.NO_BORDER;
                c2.AddElement(new Paragraph($"Mã đơn: #{order.id}", normalFont));
                c2.AddElement(new Paragraph($"Thời gian: {(order.created_at.HasValue ? order.created_at.Value.ToString("dd/MM/yyyy HH:mm") : DateTime.Now.ToString("dd/MM/yyyy HH:mm"))}", normalFont));
                string ptThanhToan = order.payment_method == "transfer" ? "Chuyển khoản (VietQR)" : "Tiền mặt (COD)";
                c2.AddElement(new Paragraph($"Thanh toán: {ptThanhToan} ({order.payment_status})", normalFont));
                infoTable.AddCell(c2);

                document.Add(infoTable);

                PdfPTable itemTable = new PdfPTable(6);
                itemTable.WidthPercentage = 100;
                itemTable.SetWidths(new float[] { 8f, 35f, 12f, 15f, 12f, 18f });
                itemTable.SpacingAfter = 15f;

                string[] headers = { "STT", "Tên thức uống", "Size", "Tùy chọn", "SL", "Thành tiền" };
                foreach (var h in headers)
                {
                    PdfPCell hCell = new PdfPCell(new Phrase(h, headerTableFont));
                    hCell.BackgroundColor = new BaseColor(30, 41, 59);
                    hCell.Padding = 6f;
                    hCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    itemTable.AddCell(hCell);
                }

                int stt = 1;
                foreach (var oi in order.order_items)
                {
                    var variant = oi.product_variants;
                    string productName = variant != null && variant.product != null ? variant.product.name : "Thức uống";
                    string size = variant != null ? variant.size : "M";

                    string toppingsStr = "";
                    if (oi.order_item_toppings != null && oi.order_item_toppings.Count > 0)
                    {
                        toppingsStr = " + " + string.Join(", ", oi.order_item_toppings.Select(t => t.topping_name));
                    }
                    string optionsStr = $"Đường {oi.sugar_level}%, Đá {oi.ice_option}{toppingsStr}";

                    decimal itemToppingsTotal = oi.order_item_toppings != null ? oi.order_item_toppings.Sum(t => t.price) : 0;
                    decimal lineTotal = (oi.unit_price + itemToppingsTotal) * oi.quantity;

                    PdfPCell cellStt = new PdfPCell(new Phrase(stt.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f };
                    PdfPCell cellName = new PdfPCell(new Phrase(productName, normalFont)) { Padding = 6f };
                    PdfPCell cellSize = new PdfPCell(new Phrase(size, normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f };
                    PdfPCell cellOpt = new PdfPCell(new Phrase(optionsStr, smallFont)) { Padding = 6f };
                    PdfPCell cellQty = new PdfPCell(new Phrase(oi.quantity.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f };
                    PdfPCell cellTotal = new PdfPCell(new Phrase($"{lineTotal:N0} đ", normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f };

                    itemTable.AddCell(cellStt);
                    itemTable.AddCell(cellName);
                    itemTable.AddCell(cellSize);
                    itemTable.AddCell(cellOpt);
                    itemTable.AddCell(cellQty);
                    itemTable.AddCell(cellTotal);

                    stt++;
                }

                document.Add(itemTable);

                PdfPTable summaryTable = new PdfPTable(2);
                summaryTable.WidthPercentage = 45;
                summaryTable.HorizontalAlignment = Element.ALIGN_RIGHT;
                summaryTable.SetWidths(new float[] { 50f, 50f });
                summaryTable.SpacingAfter = 20f;

                summaryTable.AddCell(new PdfPCell(new Phrase("Tạm tính:", normalFont)) { Border = Rectangle.NO_BORDER, Padding = 4f });
                summaryTable.AddCell(new PdfPCell(new Phrase($"{order.subtotal:N0} đ", normalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4f });

                summaryTable.AddCell(new PdfPCell(new Phrase("Phí vận chuyển:", normalFont)) { Border = Rectangle.NO_BORDER, Padding = 4f });
                summaryTable.AddCell(new PdfPCell(new Phrase($"{order.shipping_fee:N0} đ", normalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4f });

                if (order.discount_amount > 0)
                {
                    summaryTable.AddCell(new PdfPCell(new Phrase("Giảm giá:", normalFont)) { Border = Rectangle.NO_BORDER, Padding = 4f });
                    summaryTable.AddCell(new PdfPCell(new Phrase($"-{order.discount_amount:N0} đ", normalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 4f });
                }

                PdfPCell totalLabel = new PdfPCell(new Phrase("TỔNG THANH TOÁN:", boldFont)) { Border = Rectangle.TOP_BORDER, PaddingTop = 8f };
                PdfPCell totalVal = new PdfPCell(new Phrase($"{order.final_total:N0} đ", new Font(bf, 13, Font.BOLD, new BaseColor(225, 29, 72)))) { Border = Rectangle.TOP_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, PaddingTop = 8f };
                summaryTable.AddCell(totalLabel);
                summaryTable.AddCell(totalVal);

                document.Add(summaryTable);

                Paragraph thankYou = new Paragraph("Cảm ơn quý khách đã tin dùng và ủng hộ Tiệm Trà Sữa!", normalFont);
                thankYou.Alignment = Element.ALIGN_CENTER;
                thankYou.SpacingBefore = 20f;
                document.Add(thankYou);

                Paragraph footerNote = new Paragraph("Hotline hỗ trợ & Khiếu nại: 1900 1234 - cskh@trasua.vn", smallFont);
                footerNote.Alignment = Element.ALIGN_CENTER;
                document.Add(footerNote);

                document.Close();
                byte[] bytes = ms.ToArray();
                Response.AppendHeader("Content-Disposition", $"inline; filename=HoaDon_{order.id}.pdf");
                return File(bytes, "application/pdf");
            }
        }

        // ==========================================
        // 7. CÁC TRANG TĨNH THÔNG TIN (ABOUT, CONTACT, TIPS)
        // ==========================================
        public ActionResult About()
        {
            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View();
        }

        public ActionResult Tips()
        {
            ViewBag.CartCount = GetCart().Sum(item => item.Quantity);
            return View();
        }
    }
}
