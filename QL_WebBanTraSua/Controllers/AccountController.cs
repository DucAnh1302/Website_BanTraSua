using System;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;
using QL_WebBanTraSua.Models;
using QL_WebBanTraSua.Services;

namespace QL_WebBanTraSua.Controllers
{
    public class AccountController : Controller
    {
        private db_trasuaEntities db = new db_trasuaEntities();

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

        // ========================================================
        // 1. ĐĂNG NHẬP (1 BƯỚC CHO CUSTOMER, OTP CHO ADMIN / KHO / POS)
        // ========================================================
        public ActionResult DangNhap(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (Session["UserId"] != null)
            {
                ViewBag.CurrentUserName = Session["UserName"];
                ViewBag.CurrentUserRole = Session["UserRole"];
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangNhap(string email, string password, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.InputEmail = email;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                return View();
            }

            string cleanEmail = email.Trim().ToLower();
            var user = db.users.FirstOrDefault(u => u.email.ToLower() == cleanEmail);

            if (user == null || user.password != password)
            {
                InventoryService.WriteAuditLog(
                    db, Request,
                    user != null ? (long?)user.id : null,
                    cleanEmail,
                    user != null ? user.role : null,
                    "AUTH_LOGIN_FAILED",
                    "Đăng nhập thất bại với email: " + cleanEmail,
                    "User",
                    user != null ? user.id.ToString() : null
                );
                db.SaveChanges();

                TempData["Error"] = "Email hoặc mật khẩu không chính xác.";
                return View();
            }

            if (!user.status)
            {
                InventoryService.WriteAuditLog(
                    db, Request, user.id, user.email, user.role,
                    "AUTH_LOGIN_BLOCKED",
                    "Đăng nhập vào tài khoản đã bị khóa: " + user.email,
                    "User", user.id.ToString()
                );
                db.SaveChanges();

                TempData["Error"] = "Tài khoản của bạn đang bị khóa. Vui lòng liên hệ Quản trị viên.";
                return View();
            }

            string role = (user.role ?? "customer").Trim().ToLower();

            // Vai tro noi bo (admin, kho, pos, staff) bat buoc xac thuc OTP 2 buoc
            if (role == "admin" || role == "kho" || role == "pos" || role == "staff")
            {
                string otp = GenerateSixDigitOtp();
                ClearAuthSession();

                Session["OtpUserId"] = user.id;
                Session["OtpCode"] = otp;
                Session["OtpExpiresAt"] = DateTime.Now.AddMinutes(10);
                Session["OtpReturnUrl"] = returnUrl;

                InventoryService.WriteAuditLog(
                    db, Request, user.id, user.email, user.role,
                    "AUTH_2FA_CHALLENGE",
                    "Khởi tạo mã OTP 6 chữ số cho tài khoản nội bộ: " + user.email,
                    "User", user.id.ToString()
                );
                db.SaveChanges();

                TempData["Success"] = "Mật khẩu hợp lệ! Vui lòng nhập mã OTP 6 chữ số để hoàn tất xác thực.";
                return RedirectToAction("XacThucOtp");
            }

            // Khach hang: dang nhap 1 buoc vao thang he thong
            InventoryService.WriteAuditLog(
                db, Request, user.id, user.email, user.role,
                "AUTH_LOGIN_SUCCESS",
                "Khách hàng đăng nhập thành công: " + user.email,
                "User", user.id.ToString()
            );
            db.SaveChanges();

            return CompleteLoginAndRedirect(user, returnUrl);
        }

        // ========================================================
        // 2. XÁC THỰC OTP (DÀNH CHO ADMIN, KHO, POS / NHÂN VIÊN)
        // ========================================================
        public ActionResult XacThucOtp()
        {
            if (Session["OtpUserId"] == null)
            {
                TempData["Error"] = "Phiên xác thực OTP không tồn tại. Vui lòng đăng nhập lại.";
                return RedirectToAction("DangNhap");
            }

            long otpUserId = Convert.ToInt64(Session["OtpUserId"]);
            var user = db.users.Find(otpUserId);
            if (user == null || !user.status)
            {
                ClearOtpSession();
                TempData["Error"] = "Tài khoản không hợp lệ hoặc đã bị khóa.";
                return RedirectToAction("DangNhap");
            }

            ViewBag.UserEmail = user.email;
            ViewBag.UserName = user.name;
            ViewBag.UserRole = user.role;
            ViewBag.OtpCodeTest = Session["OtpCode"] as string;
            ViewBag.OtpExpiresAt = Session["OtpExpiresAt"] as DateTime?;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacThucOtp(string code)
        {
            if (Session["OtpUserId"] == null || Session["OtpCode"] == null || Session["OtpExpiresAt"] == null)
            {
                TempData["Error"] = "Phiên xác thực OTP đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("DangNhap");
            }

            long otpUserId = Convert.ToInt64(Session["OtpUserId"]);
            string expectedOtp = Session["OtpCode"].ToString();
            DateTime expiresAt = (DateTime)Session["OtpExpiresAt"];
            string returnUrl = Session["OtpReturnUrl"] as string;

            var user = db.users.Find(otpUserId);
            if (user == null || !user.status)
            {
                ClearOtpSession();
                TempData["Error"] = "Tài khoản không tồn tại hoặc đã bị khóa.";
                return RedirectToAction("DangNhap");
            }

            if (DateTime.Now > expiresAt)
            {
                InventoryService.WriteAuditLog(
                    db, Request, user.id, user.email, user.role,
                    "AUTH_2FA_EXPIRED",
                    "Mã OTP đã hết hạn khi xác thực: " + user.email,
                    "User", user.id.ToString()
                );
                db.SaveChanges();

                TempData["Error"] = "Mã OTP đã hết hạn (quá 10 phút). Vui lòng bấm 'Gửi lại mã OTP mới'.";
                return RedirectToAction("XacThucOtp");
            }

            string cleanCode = (code ?? "").Trim();
            if (!string.Equals(cleanCode, expectedOtp, StringComparison.Ordinal))
            {
                InventoryService.WriteAuditLog(
                    db, Request, user.id, user.email, user.role,
                    "AUTH_2FA_FAILED",
                    "Nhập sai mã OTP cho tài khoản: " + user.email,
                    "User", user.id.ToString()
                );
                db.SaveChanges();

                TempData["Error"] = "Mã OTP không chính xác. Vui lòng kiểm tra và thử lại.";
                return RedirectToAction("XacThucOtp");
            }

            // Huy OTP sau khi xac thuc thanh cong (chong Replay)
            ClearOtpSession();

            InventoryService.WriteAuditLog(
                db, Request, user.id, user.email, user.role,
                "AUTH_2FA_SUCCESS",
                "Xác thực OTP thành công, đăng nhập vai trò: " + (user.role ?? "").ToUpper(),
                "User", user.id.ToString()
            );
            db.SaveChanges();

            TempData["Success"] = "Xác thực OTP thành công! Chào mừng " + user.name + ".";
            return CompleteLoginAndRedirect(user, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiLaiOtp()
        {
            if (Session["OtpUserId"] == null)
            {
                return RedirectToAction("DangNhap");
            }

            long otpUserId = Convert.ToInt64(Session["OtpUserId"]);
            var user = db.users.Find(otpUserId);
            if (user == null)
            {
                ClearOtpSession();
                return RedirectToAction("DangNhap");
            }

            string newOtp = GenerateSixDigitOtp();
            Session["OtpCode"] = newOtp;
            Session["OtpExpiresAt"] = DateTime.Now.AddMinutes(10);

            InventoryService.WriteAuditLog(
                db, Request, user.id, user.email, user.role,
                "AUTH_2FA_RESEND",
                "Cấp lại mã OTP mới cho tài khoản: " + user.email,
                "User", user.id.ToString()
            );
            db.SaveChanges();

            TempData["Success"] = "Đã tạo mã OTP 6 chữ số mới!";
            return RedirectToAction("XacThucOtp");
        }

        // ========================================================
        // 3. ĐĂNG KÝ KHÁCH HÀNG MỚI
        // ========================================================
        public ActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangKy(string name, string email, string phone, string password, string confirm_password)
        {
            ViewBag.InputName = name;
            ViewBag.InputEmail = email;
            ViewBag.InputPhone = phone;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ Họ tên, Email và Mật khẩu.";
                return View();
            }

            string cleanEmail = email.Trim();
            if (db.users.Any(u => u.email.ToLower() == cleanEmail.ToLower()))
            {
                TempData["Error"] = "Email này đã được sử dụng cho một tài khoản khác.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["Error"] = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View();
            }

            if (password != confirm_password)
            {
                TempData["Error"] = "Xác nhận mật khẩu không khớp.";
                return View();
            }

            var newCustomer = new user
            {
                name = name.Trim(),
                email = cleanEmail,
                phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                role = "customer",
                status = true,
                points = 0,
                tier = "Đồng",
                password = password,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            db.users.Add(newCustomer);
            db.SaveChanges();

            InventoryService.WriteAuditLog(
                db, Request, newCustomer.id, newCustomer.email, newCustomer.role,
                "AUTH_REGISTER",
                "Khách hàng đăng ký tài khoản mới: " + newCustomer.email,
                "User", newCustomer.id.ToString()
            );
            db.SaveChanges();

            TempData["Success"] = "Đăng ký thành công! Chào mừng " + newCustomer.name + " đến với TeaBloom.";
            return CompleteLoginAndRedirect(newCustomer, null);
        }

        // ========================================================
        // 4. ĐĂNG XUẤT & TRANG TỪ CHỐI QUYỀN
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DangXuat()
        {
            if (Session["UserId"] != null)
            {
                long uid;
                if (long.TryParse(Session["UserId"].ToString(), out uid))
                {
                    InventoryService.WriteAuditLog(
                        db, Request, uid,
                        Session["UserEmail"] as string,
                        Session["UserRole"] as string,
                        "AUTH_LOGOUT",
                        "Đăng xuất khỏi hệ thống",
                        "User", uid.ToString()
                    );
                    db.SaveChanges();
                }
            }

            ClearAuthSession();
            ClearOtpSession();
            Session["CustomerName"] = null;
            Session["CustomerPhone"] = null;
            Session["CustomerEmail"] = null;

            TempData["Success"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Index", "Customer");
        }

        public ActionResult KhongCoQuyen()
        {
            ViewBag.UserRole = Session["UserRole"] as string;
            ViewBag.UserName = Session["UserName"] as string;
            return View();
        }

        public ActionResult VaoKhuVucCuaToi()
        {
            string role = (Session["UserRole"] as string ?? "").Trim().ToLower();
            return RedirectByRole(role);
        }

        // ---------- Helpers ----------
        private ActionResult CompleteLoginAndRedirect(user u, string returnUrl)
        {
            Session["UserId"] = u.id;
            Session["UserRole"] = u.role;
            Session["UserName"] = u.name;
            Session["UserEmail"] = u.email;
            Session["UserPhone"] = u.phone;

            if (string.Equals(u.role, "customer", StringComparison.OrdinalIgnoreCase))
            {
                Session["CustomerName"] = u.name;
                Session["CustomerPhone"] = u.phone;
                Session["CustomerEmail"] = u.email;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectByRole((u.role ?? "customer").Trim().ToLower());
        }

        private ActionResult RedirectByRole(string role)
        {
            switch (role)
            {
                case "admin":
                    return RedirectToAction("Index", "Admin");
                case "pos":
                case "staff":
                    return RedirectToAction("Index", "Pos");
                case "kho":
                    return RedirectToAction("Index", "Kho");
                default:
                    return RedirectToAction("Index", "Customer");
            }
        }

        private void ClearAuthSession()
        {
            Session["UserId"] = null;
            Session["UserRole"] = null;
            Session["UserName"] = null;
            Session["UserEmail"] = null;
            Session["UserPhone"] = null;
        }

        private void ClearOtpSession()
        {
            Session["OtpUserId"] = null;
            Session["OtpCode"] = null;
            Session["OtpExpiresAt"] = null;
            Session["OtpReturnUrl"] = null;
        }

        private static string GenerateSixDigitOtp()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                uint val = BitConverter.ToUInt32(bytes, 0) % 900000 + 100000;
                return val.ToString();
            }
        }
    }
}
