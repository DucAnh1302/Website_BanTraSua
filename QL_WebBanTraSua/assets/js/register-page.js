document.addEventListener("DOMContentLoaded", function () {
  loadUsers().then(() => {
    setupRegisterPage();
    checkLoggedInUser();
  });
});

function setupRegisterPage() {
  var form = document.getElementById("register-form");
  if (form) {
    // SỰ KIỆN NÚT LỆNH: Nút Đăng ký
    form.addEventListener("submit", handleRegisterPage);
  }
}

function handleRegisterPage(e) {
  e.preventDefault();

  // DOM: Lấy dữ liệu
  var fullName = document.getElementById("register-name").value.trim();
  var email = document.getElementById("register-email").value.trim();
  var phone = document.getElementById("register-phone").value.trim();
  var address = document.getElementById("register-address").value.trim();
  var password = document.getElementById("register-password").value.trim();
  var confirmPassword = document
    .getElementById("register-confirm-password")
    .value.trim();

  // VALIDATION DATA (Lệnh IF-ELSE cơ bản)
  if (fullName.length < 3) {
    alert("Lỗi: Họ và tên phải ít nhất 3 ký tự");
    return;
  }

  if (email.indexOf("@") === -1 || email.indexOf(".") === -1) {
    alert("Lỗi: Email không hợp lệ");
    return;
  }

  if (phone.length < 10) {
    alert("Lỗi: Số điện thoại không hợp lệ (cần ít nhất 10 chữ số)");
    return;
  }

  if (address.length < 5) {
    alert("Lỗi: Địa chỉ phải ít nhất 5 ký tự");
    return;
  }

  if (password.length < 6) {
    alert("Lỗi: Mật khẩu phải ít nhất 6 ký tự");
    return;
  }

  if (password !== confirmPassword) {
    alert("Lỗi: Mật khẩu xác nhận không khớp");
    return;
  }

  // THUẬT TOÁN: Kiểm tra email hoặc sđt đã tồn tại chưa (Dùng for)
  var isExist = false;
  for (var i = 0; i < users.length; i++) {
    if (users[i].email === email) {
      alert("Lỗi: Email này đã được đăng ký!");
      isExist = true;
      break;
    }
    if (users[i].phone_number === phone) {
      alert("Lỗi: Số điện thoại này đã được đăng ký!");
      isExist = true;
      break;
    }
  }
  if (isExist === true) return;

  // THUẬT TOÁN: Tự tạo ID mới lớn nhất (Tìm Max)
  var maxId = 1000;
  for (var j = 0; j < users.length; j++) {
    if (users[j].user_id > maxId) {
      maxId = users[j].user_id;
    }
  }
  var newUserId = maxId + 1;

  // Khởi tạo Object khách hàng mới
  var newUser = {
    user_id: newUserId,
    full_name: fullName,
    email: email,
    password: password,
    phone_number: phone,
    address: address,
    joined_date: new Date().toLocaleDateString(),
  };

  users.push(newUser);

  // LƯU LOCAL STORAGE
  var storedData = localStorage.getItem("registeredUsers");
  var existingRegistered = [];
  if (storedData !== null) {
    existingRegistered = JSON.parse(storedData);
  }
  existingRegistered.push(newUser);

  localStorage.setItem("registeredUsers", JSON.stringify(existingRegistered));
  localStorage.setItem("currentUser", JSON.stringify(newUser));

  alert("Đăng ký thành công! Xin chào " + fullName);
  document.getElementById("register-form").reset();

  setTimeout(function () {
    window.location.href = "../index.html";
  }, 1000);
}

function checkLoggedInUser() {
  var currentUser = localStorage.getItem("currentUser");
  if (currentUser) {
    window.location.href = "../index.html";
  }
}
