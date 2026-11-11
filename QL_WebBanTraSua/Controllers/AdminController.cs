using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QL_WebBanTraSua.Models;

namespace QL_WebBanTraSua.Controllers
{
    public class AdminController : Controller
    {
        private db_trasuaEntities db = new db_trasuaEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // ========================================================
        // 1. DASHBOARD - BẢNG ĐIỀU KHIỂN & BÁO CÁO THỐNG KÊ (UC10)
        // ========================================================
        public ActionResult Index()
        {
            ViewBag.ActiveMenu = "Dashboard";

            DateTime today = DateTime.Today;

            // Thống kê doanh thu
            decimal totalRevenue = db.orders
                .Where(o => o.order_status == "completed")
                .Select(o => (decimal?)o.final_total)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            decimal todayRevenue = db.orders
                .Where(o => o.order_status == "completed" && o.created_at >= today)
                .Select(o => (decimal?)o.final_total)
                .DefaultIfEmpty(0)
                .Sum() ?? 0;

            // Thống kê đơn hàng
            int totalOrders = db.orders.Count();
            int pendingOrders = db.orders.Count(o => o.order_status == "pending");
            int preparingOrders = db.orders.Count(o => o.order_status == "preparing");
            int shippingOrders = db.orders.Count(o => o.order_status == "shipping");
            int completedOrders = db.orders.Count(o => o.order_status == "completed");
            int cancelledOrders = db.orders.Count(o => o.order_status == "cancelled");

            // Thống kê chung
            int totalProducts = db.products.Count();
            int totalCustomers = db.users.Count(u => u.role == "customer");
            int totalStaff = db.users.Count(u => u.role != "customer");

            // Cảnh báo cạn kho (UC10: stock_quantity <= min_stock)
            var stockAlerts = db.ingredients
                .Where(i => i.stock_quantity <= i.min_stock)
                .OrderBy(i => i.stock_quantity)
                .Take(8)
                .ToList();

            // Đơn hàng gần nhất
            var recentOrders = db.orders
                .Include(o => o.user)
                .OrderByDescending(o => o.created_at)
                .Take(6)
                .ToList();

            // Đưa số liệu vào ViewBag
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.PreparingOrders = preparingOrders;
            ViewBag.ShippingOrders = shippingOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CancelledOrders = cancelledOrders;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalStaff = totalStaff;
            ViewBag.StockAlerts = stockAlerts;

            return View(recentOrders);
        }

        // ========================================================
        // 2. QUẢN LÝ SẢN PHẨM (UC02)
        // ========================================================
        public ActionResult QuanLySanPham(string search, long? category_id, string status)
        {
            ViewBag.ActiveMenu = "SanPham";

            var query = db.products
                .Include(p => p.category)
                .Include(p => p.product_variants)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(p => p.name.ToLower().Contains(s));
            }

            if (category_id.HasValue && category_id.Value > 0)
            {
                query = query.Where(p => p.category_id == category_id.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    query = query.Where(p => p.is_active == true);
                else if (status == "inactive")
                    query = query.Where(p => p.is_active == false);
            }

            var dssp = query.OrderByDescending(p => p.id).ToList();

            ViewBag.Categories = db.categories.OrderBy(c => c.name).ToList();
            ViewBag.Search = search;
            ViewBag.CategoryId = category_id;
            ViewBag.Status = status;

            return View(dssp);
        }

        // GET: Thêm sản phẩm
        public ActionResult ThemSanPham()
        {
            ViewBag.ActiveMenu = "SanPham";
            ViewBag.Categories = new SelectList(db.categories.Where(c => c.is_active).OrderBy(c => c.name).ToList(), "id", "name");
            return View();
        }

        // POST: Thêm sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(product sp, HttpPostedFileBase imageFile, decimal? priceS, decimal? priceM, decimal? priceL)
        {
            ViewBag.ActiveMenu = "SanPham";

            if (string.IsNullOrWhiteSpace(sp.name))
            {
                ModelState.AddModelError("name", "Tên sản phẩm không được để trống!");
            }

            if (sp.category_id <= 0)
            {
                ModelState.AddModelError("category_id", "Vui lòng chọn danh mục hợp lệ!");
            }

            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh nếu có
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string ext = Path.GetExtension(imageFile.FileName).ToLower();
                    string newFileName = Guid.NewGuid().ToString("N") + ext;
                    string uploadDir = Server.MapPath("~/Content/images/products/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    imageFile.SaveAs(Path.Combine(uploadDir, newFileName));
                    sp.image = "products/" + newFileName;
                }

                sp.created_at = DateTime.Now;
                sp.updated_at = DateTime.Now;
                db.products.Add(sp);
                db.SaveChanges();

                // Tạo biến thể size S, M, L nếu có nhập giá
                if (priceS.HasValue && priceS.Value > 0)
                {
                    db.product_variants.Add(new product_variants
                    {
                        product_id = sp.id,
                        size = "S",
                        price = priceS.Value,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });
                }

                if (priceM.HasValue && priceM.Value > 0)
                {
                    db.product_variants.Add(new product_variants
                    {
                        product_id = sp.id,
                        size = "M",
                        price = priceM.Value,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });
                }

                if (priceL.HasValue && priceL.Value > 0)
                {
                    db.product_variants.Add(new product_variants
                    {
                        product_id = sp.id,
                        size = "L",
                        price = priceL.Value,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });
                }

                db.SaveChanges();
                TempData["Success"] = "Thêm mới món nước '" + sp.name + "' thành công!";
                return RedirectToAction("QuanLySanPham");
            }

            ViewBag.Categories = new SelectList(db.categories.Where(c => c.is_active).OrderBy(c => c.name).ToList(), "id", "name", sp.category_id);
            return View(sp);
        }

        // GET: Sửa sản phẩm
        public ActionResult SuaSanPham(long id)
        {
            ViewBag.ActiveMenu = "SanPham";
            var sp = db.products.Include(p => p.product_variants).FirstOrDefault(p => p.id == id);
            if (sp == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm yêu cầu!";
                return RedirectToAction("QuanLySanPham");
            }

            ViewBag.Categories = new SelectList(db.categories.OrderBy(c => c.name).ToList(), "id", "name", sp.category_id);

            var vS = sp.product_variants.FirstOrDefault(v => v.size == "S");
            var vM = sp.product_variants.FirstOrDefault(v => v.size == "M");
            var vL = sp.product_variants.FirstOrDefault(v => v.size == "L");

            ViewBag.PriceS = vS != null ? vS.price : 0;
            ViewBag.PriceM = vM != null ? vM.price : 0;
            ViewBag.PriceL = vL != null ? vL.price : 0;

            return View(sp);
        }

        // POST: Sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSanPham(product sp, HttpPostedFileBase imageFile, decimal? priceS, decimal? priceM, decimal? priceL)
        {
            ViewBag.ActiveMenu = "SanPham";
            var existing = db.products.Include(p => p.product_variants).FirstOrDefault(p => p.id == sp.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction("QuanLySanPham");
            }

            if (ModelState.IsValid)
            {
                existing.name = sp.name;
                existing.category_id = sp.category_id;
                existing.description = sp.description;
                existing.is_active = sp.is_active;
                existing.updated_at = DateTime.Now;

                // Xử lý upload ảnh mới
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string ext = Path.GetExtension(imageFile.FileName).ToLower();
                    string newFileName = Guid.NewGuid().ToString("N") + ext;
                    string uploadDir = Server.MapPath("~/Content/images/products/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    imageFile.SaveAs(Path.Combine(uploadDir, newFileName));
                    existing.image = "products/" + newFileName;
                }
                else if (!string.IsNullOrEmpty(sp.image))
                {
                    existing.image = sp.image;
                }

                // Cập nhật Size S
                var varS = existing.product_variants.FirstOrDefault(v => v.size == "S");
                if (priceS.HasValue && priceS.Value > 0)
                {
                    if (varS != null)
                    {
                        varS.price = priceS.Value;
                        varS.updated_at = DateTime.Now;
                    }
                    else
                    {
                        db.product_variants.Add(new product_variants
                        {
                            product_id = existing.id,
                            size = "S",
                            price = priceS.Value,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        });
                    }
                }

                // Cập nhật Size M
                var varM = existing.product_variants.FirstOrDefault(v => v.size == "M");
                if (priceM.HasValue && priceM.Value > 0)
                {
                    if (varM != null)
                    {
                        varM.price = priceM.Value;
                        varM.updated_at = DateTime.Now;
                    }
                    else
                    {
                        db.product_variants.Add(new product_variants
                        {
                            product_id = existing.id,
                            size = "M",
                            price = priceM.Value,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        });
                    }
                }

                // Cập nhật Size L
                var varL = existing.product_variants.FirstOrDefault(v => v.size == "L");
                if (priceL.HasValue && priceL.Value > 0)
                {
                    if (varL != null)
                    {
                        varL.price = priceL.Value;
                        varL.updated_at = DateTime.Now;
                    }
                    else
                    {
                        db.product_variants.Add(new product_variants
                        {
                            product_id = existing.id,
                            size = "L",
                            price = priceL.Value,
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        });
                    }
                }

                db.SaveChanges();
                TempData["Success"] = "Cập nhật sản phẩm '" + existing.name + "' thành công!";
                return RedirectToAction("QuanLySanPham");
            }

            ViewBag.Categories = new SelectList(db.categories.OrderBy(c => c.name).ToList(), "id", "name", sp.category_id);
            return View(sp);
        }

        // POST: Xóa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaSanPham(long id)
        {
            var sp = db.products.Include(p => p.product_variants).FirstOrDefault(p => p.id == id);
            if (sp != null)
            {
                // Kiểm tra xem sản phẩm đã có trong chi tiết đơn hàng hay chưa
                var variantIds = sp.product_variants.Select(v => v.id).ToList();
                bool hasOrders = db.order_items.Any(oi => variantIds.Contains(oi.variant_id));

                if (hasOrders)
                {
                    // Chuyển sang tạm ngưng bán để đảm bảo toàn vẹn dữ liệu
                    sp.is_active = false;
                    sp.updated_at = DateTime.Now;
                    db.SaveChanges();
                    TempData["Error"] = "Sản phẩm '" + sp.name + "' đã phát sinh đơn hàng trong quá khứ! Hệ thống đã tự động chuyển sang trạng thái TẠM NGƯNG BÁN thay vì xóa vĩnh viễn.";
                }
                else
                {
                    // Xóa các biến thể trước
                    var variants = sp.product_variants.ToList();
                    foreach (var v in variants)
                    {
                        db.product_variants.Remove(v);
                    }
                    db.products.Remove(sp);
                    db.SaveChanges();
                    TempData["Success"] = "Đã xóa vĩnh viễn món nước '" + sp.name + "' thành công!";
                }
            }
            return RedirectToAction("QuanLySanPham");
        }

        // POST: Đổi trạng thái hiển thị
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleTrangThaiSanPham(long id)
        {
            var sp = db.products.Find(id);
            if (sp != null)
            {
                sp.is_active = !sp.is_active;
                sp.updated_at = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = "Đã " + (sp.is_active ? "kích hoạt mở bán" : "tạm ngưng kinh doanh") + " món: " + sp.name;
            }
            return RedirectToAction("QuanLySanPham");
        }

        // ========================================================
        // 3. QUẢN LÝ DANH MỤC SẢN PHẨM
        // ========================================================
        public ActionResult QuanLyDanhMuc(string search)
        {
            ViewBag.ActiveMenu = "DanhMuc";

            var query = db.categories.Include(c => c.products).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(c => c.name.ToLower().Contains(s));
            }

            var dsdm = query.OrderBy(c => c.id).ToList();
            ViewBag.Search = search;
            return View(dsdm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemDanhMuc(string name, bool is_active = true)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Tên danh mục không được để trống!";
                return RedirectToAction("QuanLyDanhMuc");
            }

            var dm = new category
            {
                name = name.Trim(),
                is_active = is_active,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            db.categories.Add(dm);
            db.SaveChanges();
            TempData["Success"] = "Thêm danh mục mới '" + dm.name + "' thành công!";
            return RedirectToAction("QuanLyDanhMuc");
        }

        public ActionResult SuaDanhMuc(long id)
        {
            ViewBag.ActiveMenu = "DanhMuc";
            var dm = db.categories.Find(id);
            if (dm == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục!";
                return RedirectToAction("QuanLyDanhMuc");
            }
            return View(dm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaDanhMuc(category dm)
        {
            ViewBag.ActiveMenu = "DanhMuc";
            var existing = db.categories.Find(dm.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy danh mục!";
                return RedirectToAction("QuanLyDanhMuc");
            }

            if (string.IsNullOrWhiteSpace(dm.name))
            {
                ModelState.AddModelError("name", "Tên danh mục không được để trống!");
                return View(dm);
            }

            existing.name = dm.name.Trim();
            existing.is_active = dm.is_active;
            existing.updated_at = DateTime.Now;

            db.SaveChanges();
            TempData["Success"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("QuanLyDanhMuc");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaDanhMuc(long id)
        {
            var dm = db.categories.Include(c => c.products).FirstOrDefault(c => c.id == id);
            if (dm != null)
            {
                if (dm.products.Count > 0)
                {
                    TempData["Error"] = "Không thể xóa danh mục '" + dm.name + "' vì đang chứa " + dm.products.Count + " món nước! Hãy chuyển sản phẩm sang danh mục khác trước.";
                }
                else
                {
                    db.categories.Remove(dm);
                    db.SaveChanges();
                    TempData["Success"] = "Xóa danh mục '" + dm.name + "' thành công!";
                }
            }
            return RedirectToAction("QuanLyDanhMuc");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleTrangThaiDanhMuc(long id)
        {
            var dm = db.categories.Find(id);
            if (dm != null)
            {
                dm.is_active = !dm.is_active;
                dm.updated_at = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = "Đã cập nhật trạng thái danh mục: " + dm.name;
            }
            return RedirectToAction("QuanLyDanhMuc");
        }

        // ========================================================
        // 4. QUẢN LÝ ĐƠN HÀNG (UC03, UC06, UC10)
        // ========================================================
        public ActionResult QuanLyDonHang(string status, string search)
        {
            ViewBag.ActiveMenu = "DonHang";

            var query = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.order_status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower();
                long searchId;
                if (long.TryParse(s, out searchId))
                {
                    query = query.Where(o => o.id == searchId || o.shipping_phone.Contains(s));
                }
                else
                {
                    query = query.Where(o => o.shipping_name.ToLower().Contains(s) || o.shipping_phone.Contains(s));
                }
            }

            var dsDonHang = query.OrderByDescending(o => o.created_at).ToList();

            ViewBag.Status = status;
            ViewBag.Search = search;
            return View(dsDonHang);
        }

        public ActionResult ChiTietDonHang(long id)
        {
            ViewBag.ActiveMenu = "DonHang";

            var order = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng #" + id;
                return RedirectToAction("QuanLyDonHang");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatTrangThaiDonHang(long id, string order_status, string payment_status)
        {
            var order = db.orders
                .Include(o => o.order_items.Select(oi => oi.product_variants.recipe_details.Select(rd => rd.ingredient)))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings.Select(oit => oit.topping.ingredient)))
                .FirstOrDefault(o => o.id == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("QuanLyDonHang");
            }

            string oldStatus = order.order_status;
            order.order_status = order_status;
            order.payment_status = payment_status;
            order.updated_at = DateTime.Now;

            // Xử lý hoàn kho tự động theo đặc tả UC06 nếu hủy đơn
            if (oldStatus != "cancelled" && order_status == "cancelled")
            {
                foreach (var item in order.order_items)
                {
                    // Hoàn trả nguyên liệu của ly nước
                    if (item.product_variants != null && item.product_variants.recipe_details != null)
                    {
                        foreach (var rd in item.product_variants.recipe_details)
                        {
                            if (rd.ingredient != null)
                            {
                                double refundQty = rd.amount * item.quantity;
                                rd.ingredient.stock_quantity += refundQty;

                                db.inventory_transactions.Add(new inventory_transactions
                                {
                                    ingredient_id = rd.ingredient_id,
                                    order_id = order.id,
                                    quantity_changed = refundQty,
                                    transaction_type = "import_cancel",
                                    note = "Hoàn kho tự động từ hủy đơn hàng #" + order.id,
                                    created_at = DateTime.Now
                                });
                            }
                        }
                    }

                    // Hoàn trả nguyên liệu của topping
                    if (item.order_item_toppings != null)
                    {
                        foreach (var oit in item.order_item_toppings)
                        {
                            if (oit.topping != null && oit.topping.ingredient != null && oit.topping.amount_per_serving.HasValue)
                            {
                                double toppingRefund = oit.topping.amount_per_serving.Value * item.quantity;
                                oit.topping.ingredient.stock_quantity += toppingRefund;

                                db.inventory_transactions.Add(new inventory_transactions
                                {
                                    ingredient_id = oit.topping.ingredient.id,
                                    order_id = order.id,
                                    quantity_changed = toppingRefund,
                                    transaction_type = "import_cancel",
                                    note = "Hoàn kho topping từ hủy đơn #" + order.id,
                                    created_at = DateTime.Now
                                });
                            }
                        }
                    }
                }
            }

            db.SaveChanges();
            TempData["Success"] = "Đã cập nhật trạng thái đơn hàng #" + id + " thành công!";
            return RedirectToAction("ChiTietDonHang", new { id = id });
        }

        // In hóa đơn bán lẻ (UC09)
        public ActionResult InHoaDon(long id)
        {
            var order = db.orders
                .Include(o => o.user)
                .Include(o => o.order_items.Select(oi => oi.product_variants.product))
                .Include(o => o.order_items.Select(oi => oi.order_item_toppings))
                .FirstOrDefault(o => o.id == id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy hóa đơn!";
                return RedirectToAction("QuanLyDonHang");
            }

            return View(order);
        }

        // ========================================================
        // 5. QUẢN LÝ TOPPING
        // ========================================================
        public ActionResult QuanLyTopping()
        {
            ViewBag.ActiveMenu = "Topping";
            var dsTopping = db.toppings.Include(t => t.ingredient).OrderBy(t => t.id).ToList();
            return View(dsTopping);
        }

        public ActionResult ThemTopping()
        {
            ViewBag.ActiveMenu = "Topping";
            ViewBag.Ingredients = new SelectList(db.ingredients.OrderBy(i => i.name).ToList(), "id", "name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTopping(topping tp)
        {
            ViewBag.ActiveMenu = "Topping";
            if (string.IsNullOrWhiteSpace(tp.name))
            {
                ModelState.AddModelError("name", "Tên Topping không được để trống!");
            }

            if (ModelState.IsValid)
            {
                tp.created_at = DateTime.Now;
                tp.updated_at = DateTime.Now;
                db.toppings.Add(tp);
                db.SaveChanges();
                TempData["Success"] = "Thêm mới Topping '" + tp.name + "' thành công!";
                return RedirectToAction("QuanLyTopping");
            }

            ViewBag.Ingredients = new SelectList(db.ingredients.OrderBy(i => i.name).ToList(), "id", "name", tp.ingredient_id);
            return View(tp);
        }

        public ActionResult SuaTopping(long id)
        {
            ViewBag.ActiveMenu = "Topping";
            var tp = db.toppings.Find(id);
            if (tp == null)
            {
                TempData["Error"] = "Không tìm thấy Topping!";
                return RedirectToAction("QuanLyTopping");
            }

            ViewBag.Ingredients = new SelectList(db.ingredients.OrderBy(i => i.name).ToList(), "id", "name", tp.ingredient_id);
            return View(tp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaTopping(topping tp)
        {
            ViewBag.ActiveMenu = "Topping";
            var existing = db.toppings.Find(tp.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy Topping!";
                return RedirectToAction("QuanLyTopping");
            }

            if (string.IsNullOrWhiteSpace(tp.name))
            {
                ModelState.AddModelError("name", "Tên Topping không được để trống!");
                ViewBag.Ingredients = new SelectList(db.ingredients.OrderBy(i => i.name).ToList(), "id", "name", tp.ingredient_id);
                return View(tp);
            }

            existing.name = tp.name.Trim();
            existing.price = tp.price;
            existing.ingredient_id = tp.ingredient_id;
            existing.amount_per_serving = tp.amount_per_serving;
            existing.is_active = tp.is_active;
            existing.updated_at = DateTime.Now;

            db.SaveChanges();
            TempData["Success"] = "Cập nhật Topping thành công!";
            return RedirectToAction("QuanLyTopping");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaTopping(long id)
        {
            var tp = db.toppings.Find(id);
            if (tp != null)
            {
                bool hasOrders = db.order_item_toppings.Any(oit => oit.topping_id == id);
                if (hasOrders)
                {
                    tp.is_active = false;
                    tp.updated_at = DateTime.Now;
                    db.SaveChanges();
                    TempData["Error"] = "Topping đã được khách đặt trong các đơn hàng, đã chuyển sang trạng thái TẠM ẨN!";
                }
                else
                {
                    db.toppings.Remove(tp);
                    db.SaveChanges();
                    TempData["Success"] = "Đã xóa Topping '" + tp.name + "' thành công!";
                }
            }
            return RedirectToAction("QuanLyTopping");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleTrangThaiTopping(long id)
        {
            var tp = db.toppings.Find(id);
            if (tp != null)
            {
                tp.is_active = !tp.is_active;
                tp.updated_at = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = "Đã đổi trạng thái Topping: " + tp.name;
            }
            return RedirectToAction("QuanLyTopping");
        }

        // ========================================================
        // 6. QUẢN LÝ KHO NGUYÊN LIỆU (UC05, UC07, UC10)
        // ========================================================
        public ActionResult QuanLyKho(string search, string filterStock)
        {
            ViewBag.ActiveMenu = "Kho";

            var query = db.ingredients.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(i => i.name.ToLower().Contains(s));
            }

            if (filterStock == "low")
            {
                query = query.Where(i => i.stock_quantity <= i.min_stock);
            }
            else if (filterStock == "out")
            {
                query = query.Where(i => i.stock_quantity <= 0);
            }

            var dsNguyenLieu = query.OrderBy(i => i.stock_quantity).ToList();

            // Lịch sử giao dịch kho gần nhất
            var recentTransactions = db.inventory_transactions
                .Include(t => t.ingredient)
                .OrderByDescending(t => t.created_at)
                .Take(10)
                .ToList();

            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.Search = search;
            ViewBag.FilterStock = filterStock;

            return View(dsNguyenLieu);
        }

        public ActionResult ThemNguyenLieu()
        {
            ViewBag.ActiveMenu = "Kho";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNguyenLieu(ingredient ng)
        {
            ViewBag.ActiveMenu = "Kho";
            if (string.IsNullOrWhiteSpace(ng.name))
            {
                ModelState.AddModelError("name", "Tên nguyên liệu không được để trống!");
            }
            if (string.IsNullOrWhiteSpace(ng.unit))
            {
                ModelState.AddModelError("unit", "Đơn vị tính không được để trống!");
            }

            if (ModelState.IsValid)
            {
                ng.created_at = DateTime.Now;
                ng.updated_at = DateTime.Now;
                if (string.IsNullOrEmpty(ng.consumption_role)) ng.consumption_role = "normal";

                db.ingredients.Add(ng);
                db.SaveChanges();
                TempData["Success"] = "Thêm mới nguyên liệu '" + ng.name + "' thành công!";
                return RedirectToAction("QuanLyKho");
            }
            return View(ng);
        }

        public ActionResult SuaNguyenLieu(long id)
        {
            ViewBag.ActiveMenu = "Kho";
            var ng = db.ingredients.Find(id);
            if (ng == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu!";
                return RedirectToAction("QuanLyKho");
            }
            return View(ng);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaNguyenLieu(ingredient ng)
        {
            ViewBag.ActiveMenu = "Kho";
            var existing = db.ingredients.Find(ng.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu!";
                return RedirectToAction("QuanLyKho");
            }

            if (string.IsNullOrWhiteSpace(ng.name) || string.IsNullOrWhiteSpace(ng.unit))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ tên và đơn vị tính!";
                return View(ng);
            }

            existing.name = ng.name.Trim();
            existing.unit = ng.unit.Trim();
            existing.min_stock = ng.min_stock;
            existing.consumption_role = string.IsNullOrEmpty(ng.consumption_role) ? "normal" : ng.consumption_role;
            existing.updated_at = DateTime.Now;

            db.SaveChanges();
            TempData["Success"] = "Cập nhật nguyên liệu thành công!";
            return RedirectToAction("QuanLyKho");
        }

        // Nhập kho thêm số lượng (UC07)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NhapKho(long ingredient_id, double quantity, decimal? unit_cost, string note)
        {
            var ng = db.ingredients.Find(ingredient_id);
            if (ng != null && quantity > 0)
            {
                ng.stock_quantity += quantity;
                ng.updated_at = DateTime.Now;

                decimal totalCost = (decimal)quantity * (unit_cost ?? 0);

                db.inventory_transactions.Add(new inventory_transactions
                {
                    ingredient_id = ingredient_id,
                    quantity_changed = quantity,
                    unit_cost = unit_cost,
                    total_cost = totalCost,
                    transaction_type = "import_manual",
                    note = string.IsNullOrEmpty(note) ? "Nhập kho thủ công định lượng" : note,
                    created_at = DateTime.Now
                });

                db.SaveChanges();
                TempData["Success"] = "Nhập thêm " + quantity + " " + ng.unit + " cho '" + ng.name + "' thành công!";
            }
            else
            {
                TempData["Error"] = "Số lượng nhập kho phải lớn hơn 0!";
            }
            return RedirectToAction("QuanLyKho");
        }

        // ========================================================
        // 7. QUẢN LÝ KHUYẾN MÃI (VOUCHER) (UC08)
        // ========================================================
        public ActionResult QuanLyVoucher()
        {
            ViewBag.ActiveMenu = "Voucher";
            var dsVoucher = db.vouchers.OrderByDescending(v => v.id).ToList();
            return View(dsVoucher);
        }

        public ActionResult ThemVoucher()
        {
            ViewBag.ActiveMenu = "Voucher";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemVoucher(voucher vc)
        {
            ViewBag.ActiveMenu = "Voucher";

            if (string.IsNullOrWhiteSpace(vc.code))
            {
                ModelState.AddModelError("code", "Mã voucher không được để trống!");
            }
            else
            {
                vc.code = vc.code.Trim().ToUpper();
                if (db.vouchers.Any(v => v.code == vc.code))
                {
                    ModelState.AddModelError("code", "Mã voucher này đã tồn tại trong hệ thống!");
                }
            }

            if (string.IsNullOrWhiteSpace(vc.name))
            {
                ModelState.AddModelError("name", "Tên chương trình khuyến mãi không được để trống!");
            }

            if (ModelState.IsValid)
            {
                vc.used_count = 0;
                vc.created_at = DateTime.Now;
                vc.updated_at = DateTime.Now;

                db.vouchers.Add(vc);
                db.SaveChanges();
                TempData["Success"] = "Tạo mã voucher mới '" + vc.code + "' thành công!";
                return RedirectToAction("QuanLyVoucher");
            }

            return View(vc);
        }

        public ActionResult SuaVoucher(long id)
        {
            ViewBag.ActiveMenu = "Voucher";
            var vc = db.vouchers.Find(id);
            if (vc == null)
            {
                TempData["Error"] = "Không tìm thấy voucher!";
                return RedirectToAction("QuanLyVoucher");
            }
            return View(vc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaVoucher(voucher vc)
        {
            ViewBag.ActiveMenu = "Voucher";
            var existing = db.vouchers.Find(vc.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy voucher!";
                return RedirectToAction("QuanLyVoucher");
            }

            if (string.IsNullOrWhiteSpace(vc.name))
            {
                ModelState.AddModelError("name", "Tên chương trình không được để trống!");
                return View(vc);
            }

            existing.name = vc.name.Trim();
            existing.discount_type = vc.discount_type;
            existing.discount_value = vc.discount_value;
            existing.max_discount = vc.max_discount;
            existing.min_order_value = vc.min_order_value;
            existing.required_tier = vc.required_tier;
            existing.usage_limit = vc.usage_limit;
            existing.expires_at = vc.expires_at;
            existing.is_active = vc.is_active;
            existing.updated_at = DateTime.Now;

            db.SaveChanges();
            TempData["Success"] = "Cập nhật mã voucher '" + existing.code + "' thành công!";
            return RedirectToAction("QuanLyVoucher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaVoucher(long id)
        {
            var vc = db.vouchers.Find(id);
            if (vc != null)
            {
                db.vouchers.Remove(vc);
                db.SaveChanges();
                TempData["Success"] = "Xóa mã voucher thành công!";
            }
            return RedirectToAction("QuanLyVoucher");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleTrangThaiVoucher(long id)
        {
            var vc = db.vouchers.Find(id);
            if (vc != null)
            {
                vc.is_active = !vc.is_active;
                vc.updated_at = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = "Đã đổi trạng thái mã voucher: " + vc.code;
            }
            return RedirectToAction("QuanLyVoucher");
        }

        // ========================================================
        // 8. QUẢN LÝ NGƯỜI DÙNG & NHÂN VIÊN
        // ========================================================
        public ActionResult QuanLyNguoiDung(string role, string search)
        {
            ViewBag.ActiveMenu = "NguoiDung";

            var query = db.users.AsQueryable();

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.role == role);
            }

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(u => u.name.ToLower().Contains(s) || u.email.ToLower().Contains(s) || u.phone.Contains(s));
            }

            var dsUsers = query.OrderByDescending(u => u.id).ToList();

            ViewBag.CurrentRole = role;
            ViewBag.Search = search;

            return View(dsUsers);
        }

        public ActionResult ThemNhanVien()
        {
            ViewBag.ActiveMenu = "NguoiDung";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNhanVien(user u)
        {
            ViewBag.ActiveMenu = "NguoiDung";

            if (string.IsNullOrWhiteSpace(u.email))
            {
                ModelState.AddModelError("email", "Email không được để trống!");
            }
            else if (db.users.Any(x => x.email == u.email.Trim()))
            {
                ModelState.AddModelError("email", "Email này đã được sử dụng!");
            }

            if (string.IsNullOrWhiteSpace(u.password))
            {
                ModelState.AddModelError("password", "Mật khẩu không được để trống!");
            }

            if (ModelState.IsValid)
            {
                u.email = u.email.Trim();
                u.name = u.name.Trim();
                u.status = true;
                u.points = 0;
                u.tier = "Đồng";
                u.created_at = DateTime.Now;
                u.updated_at = DateTime.Now;

                db.users.Add(u);
                db.SaveChanges();
                TempData["Success"] = "Tạo tài khoản nhân sự '" + u.name + "' (" + u.role + ") thành công!";
                return RedirectToAction("QuanLyNguoiDung", new { role = u.role });
            }

            return View(u);
        }

        public ActionResult SuaNguoiDung(long id)
        {
            ViewBag.ActiveMenu = "NguoiDung";
            var u = db.users.Find(id);
            if (u == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("QuanLyNguoiDung");
            }
            return View(u);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaNguoiDung(user u, string new_password)
        {
            ViewBag.ActiveMenu = "NguoiDung";
            var existing = db.users.Find(u.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("QuanLyNguoiDung");
            }

            if (string.IsNullOrWhiteSpace(u.name))
            {
                ModelState.AddModelError("name", "Họ tên không được để trống!");
                return View(u);
            }

            existing.name = u.name.Trim();
            existing.phone = u.phone;
            existing.role = u.role;
            existing.status = u.status;
            existing.points = u.points;
            if (!string.IsNullOrEmpty(u.tier)) existing.tier = u.tier;
            existing.updated_at = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(new_password))
            {
                existing.password = new_password; // Hoặc băm nếu có cơ chế hash
            }

            db.SaveChanges();
            TempData["Success"] = "Cập nhật thông tin tài khoản '" + existing.name + "' thành công!";
            return RedirectToAction("QuanLyNguoiDung");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleTrangThaiNguoiDung(long id)
        {
            var u = db.users.Find(id);
            if (u != null)
            {
                u.status = !u.status;
                u.updated_at = DateTime.Now;
                db.SaveChanges();
                TempData["Success"] = (u.status ? "Đã mở khóa tài khoản: " : "Đã khóa tài khoản: ") + u.name;
            }
            return RedirectToAction("QuanLyNguoiDung");
        }
    }
}
