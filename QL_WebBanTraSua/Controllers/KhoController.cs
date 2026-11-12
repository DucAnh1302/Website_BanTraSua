using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using QL_WebBanTraSua.Models;

namespace QL_WebBanTraSua.Controllers
{
    // Phân hệ NHÂN VIÊN KHO (Thương): quản lý nguyên liệu, nhập/xuất hàng, nhập nhanh bằng CSV
    public class KhoController : Controller
    {
        private db_trasuaEntities db = new db_trasuaEntities();

        // Các loại giao dịch kho (cột transaction_type)
        private const string LoaiNhapPhieu = "import_invoice";   // nhập theo phiếu nhập / hóa đơn
        private const string LoaiNhapCsv = "import_csv";         // nhập nhanh bằng file CSV
        private const string LoaiXuatThuCong = "export_manual";  // xuất kho thủ công (hao hụt, hủy...)

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        // ========================================================
        // 1. QUẢN LÝ NGUYÊN LIỆU: số lượng hiện tại + đơn vị
        // ========================================================
        public ActionResult Index(string search, string filterStock)
        {
            ViewBag.ActiveMenu = "NguyenLieu";
            ViewBag.Title = "Nguyên liệu";

            var query = db.ingredients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(i => i.name.ToLower().Contains(s));
            }
            if (filterStock == "low")
                query = query.Where(i => i.stock_quantity <= i.min_stock && i.stock_quantity > 0);
            else if (filterStock == "out")
                query = query.Where(i => i.stock_quantity <= 0);

            ViewBag.Search = search;
            ViewBag.FilterStock = filterStock;
            ViewBag.TongSo = db.ingredients.Count();
            ViewBag.SapHet = db.ingredients.Count(i => i.stock_quantity <= i.min_stock && i.stock_quantity > 0);
            ViewBag.HetHang = db.ingredients.Count(i => i.stock_quantity <= 0);
            ViewBag.GanDay = db.inventory_transactions.Include(x => x.ingredient)
                .OrderByDescending(x => x.created_at).Take(8).ToList();

            return View(query.OrderBy(i => i.name).ToList());
        }

        public ActionResult ThemNguyenLieu()
        {
            ViewBag.ActiveMenu = "NguyenLieu";
            ViewBag.Title = "Thêm nguyên liệu";
            return View(new ingredient { consumption_role = "normal" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNguyenLieu(ingredient ng)
        {
            ViewBag.ActiveMenu = "NguyenLieu";
            ViewBag.Title = "Thêm nguyên liệu";

            if (string.IsNullOrWhiteSpace(ng.name))
                ModelState.AddModelError("name", "Tên nguyên liệu không được để trống.");
            if (string.IsNullOrWhiteSpace(ng.unit))
                ModelState.AddModelError("unit", "Đơn vị tính không được để trống.");
            if (ng.min_stock < 0)
                ModelState.AddModelError("min_stock", "Ngưỡng cảnh báo không được âm.");
            if (!string.IsNullOrWhiteSpace(ng.name) &&
                db.ingredients.Any(i => i.name.ToLower() == ng.name.Trim().ToLower()))
                ModelState.AddModelError("name", "Nguyên liệu này đã có trong kho.");

            if (!ModelState.IsValid) return View(ng);

            // Tồn kho luôn bắt đầu từ 0, số lượng chỉ thay đổi qua phiếu nhập/xuất để có lịch sử
            ng.name = ng.name.Trim();
            ng.unit = ng.unit.Trim();
            ng.stock_quantity = 0;
            if (string.IsNullOrEmpty(ng.consumption_role)) ng.consumption_role = "normal";
            ng.created_at = DateTime.Now;
            ng.updated_at = DateTime.Now;
            db.ingredients.Add(ng);
            db.SaveChanges();

            TempData["Success"] = "Đã thêm nguyên liệu '" + ng.name + "'. Hãy lập phiếu nhập để thêm số lượng.";
            return RedirectToAction("Index");
        }

        public ActionResult SuaNguyenLieu(long id)
        {
            ViewBag.ActiveMenu = "NguyenLieu";
            ViewBag.Title = "Sửa nguyên liệu";
            var ng = db.ingredients.Find(id);
            if (ng == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu.";
                return RedirectToAction("Index");
            }
            return View(ng);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaNguyenLieu(ingredient ng)
        {
            ViewBag.ActiveMenu = "NguyenLieu";
            ViewBag.Title = "Sửa nguyên liệu";

            var existing = db.ingredients.Find(ng.id);
            if (existing == null)
            {
                TempData["Error"] = "Không tìm thấy nguyên liệu.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(ng.name))
                ModelState.AddModelError("name", "Tên nguyên liệu không được để trống.");
            if (string.IsNullOrWhiteSpace(ng.unit))
                ModelState.AddModelError("unit", "Đơn vị tính không được để trống.");
            if (ng.min_stock < 0)
                ModelState.AddModelError("min_stock", "Ngưỡng cảnh báo không được âm.");
            if (!string.IsNullOrWhiteSpace(ng.name) &&
                db.ingredients.Any(i => i.id != ng.id && i.name.ToLower() == ng.name.Trim().ToLower()))
                ModelState.AddModelError("name", "Đã có nguyên liệu khác trùng tên.");

            if (!ModelState.IsValid)
            {
                ng.stock_quantity = existing.stock_quantity;
                return View(ng);
            }

            existing.name = ng.name.Trim();
            existing.unit = ng.unit.Trim();
            existing.min_stock = ng.min_stock;
            existing.consumption_role = string.IsNullOrEmpty(ng.consumption_role) ? "normal" : ng.consumption_role;
            existing.updated_at = DateTime.Now;
            db.SaveChanges();

            TempData["Success"] = "Đã cập nhật nguyên liệu '" + existing.name + "'.";
            return RedirectToAction("Index");
        }

        // ========================================================
        // 2. NHẬP HÀNG THEO PHIẾU NHẬP (nhập tay, giá mỗi lần có thể khác)
        // ========================================================
        public ActionResult NhapHang(long? id)
        {
            ViewBag.ActiveMenu = "NhapHang";
            ViewBag.Title = "Nhập hàng";
            ViewBag.ChonId = id;
            ViewBag.DsNguyenLieu = db.ingredients.OrderBy(i => i.name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NhapHang(string soPhieu, string nhaCungCap, long[] ingredient_id, double[] quantity, decimal[] unit_cost)
        {
            if (ingredient_id == null || quantity == null || unit_cost == null ||
                ingredient_id.Length != quantity.Length || quantity.Length != unit_cost.Length)
            {
                TempData["Error"] = "Dữ liệu phiếu nhập không hợp lệ.";
                return RedirectToAction("NhapHang");
            }

            // Gom các dòng hợp lệ; dòng bỏ trống (chưa chọn nguyên liệu) sẽ bị bỏ qua
            var dong = new List<Tuple<long, double, decimal>>();
            for (int i = 0; i < ingredient_id.Length; i++)
            {
                if (ingredient_id[i] <= 0) continue;
                if (quantity[i] <= 0)
                {
                    TempData["Error"] = "Dòng " + (i + 1) + ": số lượng phải lớn hơn 0.";
                    return RedirectToAction("NhapHang");
                }
                if (unit_cost[i] < 0)
                {
                    TempData["Error"] = "Dòng " + (i + 1) + ": đơn giá không được âm.";
                    return RedirectToAction("NhapHang");
                }
                dong.Add(Tuple.Create(ingredient_id[i], quantity[i], unit_cost[i]));
            }

            if (dong.Count == 0)
            {
                TempData["Error"] = "Phiếu nhập chưa có dòng nguyên liệu nào.";
                return RedirectToAction("NhapHang");
            }

            string ghiChu = "Phiếu nhập";
            if (!string.IsNullOrWhiteSpace(soPhieu)) ghiChu += " " + soPhieu.Trim();
            if (!string.IsNullOrWhiteSpace(nhaCungCap)) ghiChu += " - NCC: " + nhaCungCap.Trim();

            decimal tongTien = 0;
            foreach (var d in dong)
            {
                var ng = db.ingredients.Find(d.Item1);
                if (ng == null)
                {
                    TempData["Error"] = "Có nguyên liệu không tồn tại trong kho.";
                    return RedirectToAction("NhapHang");
                }
                decimal thanhTien = (decimal)d.Item2 * d.Item3;
                tongTien += thanhTien;

                ng.stock_quantity += d.Item2;
                ng.updated_at = DateTime.Now;
                db.inventory_transactions.Add(new inventory_transactions
                {
                    ingredient_id = ng.id,
                    quantity_changed = d.Item2,
                    unit_cost = d.Item3,
                    total_cost = thanhTien,
                    transaction_type = LoaiNhapPhieu,
                    note = ghiChu,
                    created_at = DateTime.Now
                });
            }
            db.SaveChanges();

            TempData["Success"] = "Đã nhập " + dong.Count + " mặt hàng, tổng giá trị " + tongTien.ToString("N0", new CultureInfo("vi-VN")) + " đ.";
            return RedirectToAction("LichSu");
        }

        // ========================================================
        // 3. XUẤT HÀNG THỦ CÔNG (hao hụt, hết hạn, hỏng, điều chỉnh...)
        // ========================================================
        public ActionResult XuatHang()
        {
            ViewBag.ActiveMenu = "XuatHang";
            ViewBag.Title = "Xuất hàng";
            ViewBag.DsNguyenLieu = db.ingredients.OrderBy(i => i.name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XuatHang(long ingredient_id, double quantity, string lyDo, string note)
        {
            var ng = db.ingredients.Find(ingredient_id);
            if (ng == null)
            {
                TempData["Error"] = "Vui lòng chọn nguyên liệu cần xuất.";
                return RedirectToAction("XuatHang");
            }
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng xuất phải lớn hơn 0.";
                return RedirectToAction("XuatHang");
            }
            if (quantity > ng.stock_quantity)
            {
                TempData["Error"] = "Không đủ tồn kho: '" + ng.name + "' chỉ còn " + ng.stock_quantity + " " + ng.unit + ".";
                return RedirectToAction("XuatHang");
            }

            string ghiChu = string.IsNullOrWhiteSpace(lyDo) ? "Xuất kho" : lyDo.Trim();
            if (!string.IsNullOrWhiteSpace(note)) ghiChu += ": " + note.Trim();

            ng.stock_quantity -= quantity;
            ng.updated_at = DateTime.Now;
            db.inventory_transactions.Add(new inventory_transactions
            {
                ingredient_id = ng.id,
                quantity_changed = -quantity,   // số âm = xuất kho
                transaction_type = LoaiXuatThuCong,
                note = ghiChu,
                created_at = DateTime.Now
            });
            db.SaveChanges();

            TempData["Success"] = "Đã xuất " + quantity + " " + ng.unit + " '" + ng.name + "'. Tồn còn lại: " + ng.stock_quantity + " " + ng.unit + ".";
            return RedirectToAction("LichSu");
        }

        // ========================================================
        // 4. LỊCH SỬ NHẬP / XUẤT
        // ========================================================
        public ActionResult LichSu(string loai, long? ingredient_id, DateTime? tuNgay, DateTime? denNgay)
        {
            ViewBag.ActiveMenu = "LichSu";
            ViewBag.Title = "Lịch sử nhập xuất";

            var query = db.inventory_transactions.Include(t => t.ingredient).AsQueryable();

            if (loai == "nhap") query = query.Where(t => t.quantity_changed > 0);
            else if (loai == "xuat") query = query.Where(t => t.quantity_changed < 0);

            if (ingredient_id.HasValue)
                query = query.Where(t => t.ingredient_id == ingredient_id.Value);
            if (tuNgay.HasValue)
            {
                DateTime tu = tuNgay.Value.Date;
                query = query.Where(t => t.created_at >= tu);
            }
            if (denNgay.HasValue)
            {
                DateTime den = denNgay.Value.Date.AddDays(1);
                query = query.Where(t => t.created_at < den);
            }

            ViewBag.Loai = loai;
            ViewBag.IngredientId = ingredient_id;
            ViewBag.TuNgay = tuNgay;
            ViewBag.DenNgay = denNgay;
            ViewBag.DsNguyenLieu = db.ingredients.OrderBy(i => i.name).ToList();

            ViewBag.SoGiaoDich = query.Count();
            ViewBag.SoLanNhap = query.Count(t => t.quantity_changed > 0);
            ViewBag.SoLanXuat = query.Count(t => t.quantity_changed < 0);
            ViewBag.TongTienNhap = query.Where(t => t.quantity_changed > 0).Select(t => t.total_cost).Sum() ?? 0m;

            return View(query.OrderByDescending(t => t.created_at).Take(300).ToList());
        }

        // ========================================================
        // 5. NHẬP NHANH BẰNG FILE CSV
        // Cột: ten_nguyen_lieu, so_luong, don_gia, ghi_chu  (+ don_vi nếu muốn tạo nguyên liệu mới)
        // ========================================================
        public ActionResult NhapCsv()
        {
            ViewBag.ActiveMenu = "NhapCsv";
            ViewBag.Title = "Nhập nhanh bằng CSV";
            return View();
        }

        public ActionResult TaiMauCsv()
        {
            string noiDung =
                "ten_nguyen_lieu,so_luong,don_gia,don_vi,ghi_chu\r\n" +
                "Trà đen,2000,150,g,Phiếu nhập PN001\r\n" +
                "Sữa tươi,10000,32,ml,Phiếu nhập PN001\r\n";
            // BOM để Excel mở đúng tiếng Việt
            var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(noiDung)).ToArray();
            return File(bytes, "text/csv", "mau_nhap_kho.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NhapCsv(HttpPostedFileBase file)
        {
            ViewBag.ActiveMenu = "NhapCsv";
            ViewBag.Title = "Nhập nhanh bằng CSV";

            if (file == null || file.ContentLength == 0)
            {
                TempData["Error"] = "Vui lòng chọn file CSV.";
                return View();
            }
            if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Chỉ nhận file có đuôi .csv.";
                return View();
            }
            if (file.ContentLength > 1024 * 1024)
            {
                TempData["Error"] = "File quá lớn (tối đa 1 MB).";
                return View();
            }

            string text;
            using (var reader = new StreamReader(file.InputStream, Encoding.UTF8, true))
                text = reader.ReadToEnd();

            var rows = ParseCsv(text);
            if (rows.Count < 2)
            {
                TempData["Error"] = "File không có dòng dữ liệu nào.";
                return View();
            }

            // Xác định vị trí các cột theo dòng tiêu đề
            var header = rows[0].Select(h => h.Trim().ToLower()).ToList();
            int cTen = header.IndexOf("ten_nguyen_lieu");
            int cSl = header.IndexOf("so_luong");
            int cGia = header.IndexOf("don_gia");
            int cDv = header.IndexOf("don_vi");
            int cGhiChu = header.IndexOf("ghi_chu");
            if (cTen < 0 || cSl < 0 || cGia < 0)
            {
                TempData["Error"] = "Dòng tiêu đề phải có các cột: ten_nguyen_lieu, so_luong, don_gia. Hãy tải file mẫu để làm theo.";
                return View();
            }

            var loi = new List<string>();
            var hopLe = new List<CsvDong>();
            for (int i = 1; i < rows.Count; i++)
            {
                var r = rows[i];
                int soDong = i + 1;
                if (r.All(c => string.IsNullOrWhiteSpace(c))) continue;

                string ten = Lay(r, cTen);
                string slStr = Lay(r, cSl);
                string giaStr = Lay(r, cGia);
                if (string.IsNullOrWhiteSpace(ten)) { loi.Add("Dòng " + soDong + ": thiếu tên nguyên liệu."); continue; }

                double sl; decimal gia;
                if (!double.TryParse(slStr, NumberStyles.Any, CultureInfo.InvariantCulture, out sl) || sl <= 0)
                { loi.Add("Dòng " + soDong + " (" + ten + "): số lượng phải là số lớn hơn 0."); continue; }
                if (!decimal.TryParse(giaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out gia) || gia < 0)
                { loi.Add("Dòng " + soDong + " (" + ten + "): đơn giá phải là số không âm."); continue; }

                hopLe.Add(new CsvDong
                {
                    SoDong = soDong,
                    Ten = ten.Trim(),
                    SoLuong = sl,
                    DonGia = gia,
                    DonVi = Lay(r, cDv),
                    GhiChu = Lay(r, cGhiChu)
                });
            }

            // Đối chiếu với kho (không phân biệt hoa thường)
            var tatCa = db.ingredients.ToList();
            foreach (var d in hopLe)
            {
                d.NguyenLieu = tatCa.FirstOrDefault(i => string.Equals(i.name.Trim(), d.Ten, StringComparison.OrdinalIgnoreCase));
                if (d.NguyenLieu == null && string.IsNullOrWhiteSpace(d.DonVi))
                    loi.Add("Dòng " + d.SoDong + " (" + d.Ten + "): chưa có trong kho. Điền cột don_vi nếu muốn tạo mới.");
            }

            // Có lỗi thì không nhập dòng nào, tránh nhập dở dang
            if (loi.Count > 0)
            {
                ViewBag.DanhSachLoi = loi;
                TempData["Error"] = "File có " + loi.Count + " dòng lỗi, chưa nhập gì vào kho. Sửa file rồi tải lại.";
                return View();
            }
            if (hopLe.Count == 0)
            {
                TempData["Error"] = "File không có dòng dữ liệu hợp lệ.";
                return View();
            }

            decimal tongTien = 0;
            int taoMoi = 0;
            foreach (var d in hopLe)
            {
                var ng = d.NguyenLieu;
                if (ng == null)
                {
                    // Nhiều dòng cùng tên mới: dùng lại nguyên liệu vừa tạo
                    ng = tatCa.FirstOrDefault(i => string.Equals(i.name.Trim(), d.Ten, StringComparison.OrdinalIgnoreCase));
                    if (ng == null)
                    {
                        ng = new ingredient
                        {
                            name = d.Ten,
                            unit = d.DonVi.Trim(),
                            stock_quantity = 0,
                            min_stock = 0,
                            consumption_role = "normal",
                            created_at = DateTime.Now,
                            updated_at = DateTime.Now
                        };
                        db.ingredients.Add(ng);
                        tatCa.Add(ng);
                        taoMoi++;
                    }
                }

                decimal thanhTien = (decimal)d.SoLuong * d.DonGia;
                tongTien += thanhTien;
                ng.stock_quantity += d.SoLuong;
                ng.updated_at = DateTime.Now;

                // Gán qua navigation để EF tự lấy id của nguyên liệu mới tạo
                db.inventory_transactions.Add(new inventory_transactions
                {
                    ingredient = ng,
                    quantity_changed = d.SoLuong,
                    unit_cost = d.DonGia,
                    total_cost = thanhTien,
                    transaction_type = LoaiNhapCsv,
                    note = string.IsNullOrWhiteSpace(d.GhiChu) ? "Nhập nhanh từ file CSV" : d.GhiChu.Trim(),
                    created_at = DateTime.Now
                });
            }
            db.SaveChanges();

            string kq = "Nhập thành công " + hopLe.Count + " dòng, tổng giá trị " + tongTien.ToString("N0", new CultureInfo("vi-VN")) + " đ.";
            if (taoMoi > 0) kq += " Đã tạo mới " + taoMoi + " nguyên liệu.";
            TempData["Success"] = kq;
            return RedirectToAction("LichSu");
        }

        // ---------- Hàm hỗ trợ ----------
        private class CsvDong
        {
            public int SoDong;
            public string Ten;
            public double SoLuong;
            public decimal DonGia;
            public string DonVi;
            public string GhiChu;
            public ingredient NguyenLieu;
        }

        private static string Lay(List<string> r, int i)
        {
            return (i >= 0 && i < r.Count) ? (r[i] ?? "").Trim() : "";
        }

        // Đọc CSV đơn giản, hỗ trợ ô đặt trong dấu ngoặc kép, dấu phẩy hoặc chấm phẩy làm phân cách
        private static List<List<string>> ParseCsv(string text)
        {
            var result = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return result;
            text = text.TrimStart('\uFEFF');

            string firstLine = text.Split('\n')[0];
            char sep = firstLine.Count(c => c == ';') > firstLine.Count(c => c == ',') ? ';' : ',';

            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else cell.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == sep) { row.Add(cell.ToString()); cell.Clear(); }
                else if (c == '\r') { }
                else if (c == '\n') { row.Add(cell.ToString()); cell.Clear(); result.Add(row); row = new List<string>(); }
                else cell.Append(c);
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); result.Add(row); }
            return result;
        }
    }
}
