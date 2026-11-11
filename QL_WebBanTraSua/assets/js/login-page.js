document.addEventListener("DOMContentLoaded", function () {
  loadUsers().then(() => {
    setupLoginPage();
    checkLoggedInUser();
  });
});

function setupLoginPage() {
  var form = document.getElementById("login-form");
  if (form) {
    // 1. SỰ KIỆN TRÊN NÚT LỆNH
    form.addEventListener("submit", handleLoginPage);
  }
}

function handleLoginPage(e) {
  e.preventDefault(); // Ngăn load trang

  // 2. DOM: Lấy dữ liệu
  var loginInput = document.getElementById("login-phone").value.trim();
  var password = document.getElementById("login-password").value.trim();

  // 3. VALIDATION CƠ BẢN
  if (loginInput === "") {
    alert("Vui lòng nhập số điện thoại hoặc email");
    return;
  }
  if (password === "") {
    alert("Vui lòng nhập mật khẩu");
    return;
  }

  // Xử lý đăng nhập dành riêng cho Admin
  if (loginInput === "admin" && password === "123456") {
    var adminUser = {
      user_id: "admin_01",
      full_name: "Quản Trị Viên",
      role: "admin"
    };
    localStorage.setItem("currentUser", JSON.stringify(adminUser));
    alert("Đăng nhập quyền Quản trị viên thành công!");
    window.location.href = "admin.html";
    return;
  }

  // THUẬT TOÁN TÌM KIẾM (Vòng lặp for cơ bản nhất)
  var userFound = null;
  for (var i = 0; i < users.length; i++) {
    if (
      (users[i].phone_number === loginInput || users[i].email === loginInput) &&
      users[i].password === password
    ) {
      userFound = users[i];
      break; // Tìm thấy thì dừng vòng lặp
    }
  }

  if (userFound !== null) {
    // 4. LƯU LOCALSTORAGE VÀ SESSION STORAGE
    localStorage.setItem("currentUser", JSON.stringify(userFound));
    sessionStorage.setItem("lastLoginTime", new Date().toLocaleString());

    alert("Đăng nhập thành công! Xin chào " + userFound.full_name);

    setTimeout(function () {
      window.location.href = "../index.html";
    }, 1000);
  } else {
    alert("Thông tin đăng nhập hoặc mật khẩu không chính xác");
  }
}

function checkLoggedInUser() {
  var currentUser = localStorage.getItem("currentUser");
  if (currentUser) {
    var user = JSON.parse(currentUser);
    if (user.role === "admin") {
      window.location.href = "admin.html";
    } else {
      window.location.href = "../index.html";
    }
  }
}
