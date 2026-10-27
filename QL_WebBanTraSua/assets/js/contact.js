// ==========================================================
// BẢN CODE JAVASCRIPT SIÊU CƠ BẢN DÀNH CHO BÁO CÁO ĐỒ ÁN
// ==========================================================
document.addEventListener("DOMContentLoaded", function () {
  // 1. Dùng DOM để tìm form liên hệ
  var form = document.getElementById("contact-form");

  // 2. SỰ KIỆN TRÊN NÚT LỆNH: Bắt sự kiện khi người dùng bấm nút Gửi
  if (form) {
    form.addEventListener("submit", function (e) {
      e.preventDefault(); // Chặn hành động tải lại trang mặc định của HTML

      // DOM: Lấy giá trị mà người dùng đã gõ vào ô input
      var name = document.getElementById("contact-name").value;
      var email = document.getElementById("contact-email").value;
      var phone = document.getElementById("contact-phone").value;
      var message = document.getElementById("contact-message").value;

      // 3. VALIDATION DATA: Kiểm tra dữ liệu siêu cơ bản (dùng if-else thuần)
      if (name.length < 3) {
        alert("Lỗi: Họ và tên phải có ít nhất 3 ký tự!");
        return; // Có lỗi thì dừng lại không chạy tiếp
      }
      // Check email phải có @ và dấu chấm
      if (email.indexOf("@") === -1 || email.indexOf(".") === -1) {
        alert("Lỗi: Email không hợp lệ!");
        return;
      }
      if (phone.length < 10) {
        alert("Lỗi: Số điện thoại phải có ít nhất 10 số!");
        return;
      }
      if (message.length === 0) {
        alert("Lỗi: Vui lòng nhập nội dung!");
        return;
      }

      // 4. BIẾN SESSION & LOCALSTORAGE
      // Lưu tên người gửi vào Session (Chỉ tồn tại khi đang mở tab này)
      sessionStorage.setItem("nguoiGuiCuoiCung", name);

      // Lưu LocalStorage (Lưu vĩnh viễn trên trình duyệt để Admin có thể xem)
      var thongTinLienHe = {
        ten: name,
        thu: email,
        sdt: phone,
        tinNhan: message,
      };
      localStorage.setItem("duLieuLienHe", JSON.stringify(thongTinLienHe));

      // Báo cáo thành công và xóa trắng form
      alert(
        "Gửi thành công! Cảm ơn bạn " +
          sessionStorage.getItem("nguoiGuiCuoiCung"),
      );
      form.reset();
    });
  }
});
