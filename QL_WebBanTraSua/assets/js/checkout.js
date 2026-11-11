// ==========================================================
// TIỆM TRÀ SỮA - XỬ LÝ ĐẶT HÀNG & THANH TOÁN (checkout.js)
// Hiển thị tóm tắt đơn F&B, Validate form, Lưu lịch sử đơn hàng
// ==========================================================

document.addEventListener("DOMContentLoaded", function () {
  var cartItemsContainer = document.getElementById("checkout-cart-items");
  var checkoutSubtotal = document.getElementById("checkout-subtotal");
  var checkoutTotal = document.getElementById("checkout-total");
  var checkoutShipping = document.getElementById("checkout-shipping");

  // 1. ĐỌC DỮ LIỆU TỪ LOCALSTORAGE
  var savedCart = localStorage.getItem("cart");
  var cart = [];
  if (savedCart !== null) {
    try {
      cart = JSON.parse(savedCart);
    } catch (e) {
      cart = [];
    }
  }

  // Tự động điền thông tin người dùng nếu đã đăng nhập
  var currentUserData = localStorage.getItem("currentUser");
  if (currentUserData) {
    try {
      var user = JSON.parse(currentUserData);
      var nameField = document.getElementById("checkout-name");
      var phoneField = document.getElementById("checkout-phone");
      var emailField = document.getElementById("checkout-email");
      var addressField = document.getElementById("checkout-address");

      if (nameField && user.full_name) nameField.value = user.full_name;
      if (phoneField && user.phone_number) phoneField.value = user.phone_number;
      if (emailField && user.email) emailField.value = user.email;
      if (addressField && user.address) addressField.value = user.address;
    } catch (e) {}
  }

  // Nếu giỏ hàng trống
  if (cart.length === 0) {
    if (cartItemsContainer) {
      cartItemsContainer.innerHTML =
        '<div class="text-center py-4 text-muted">' +
        '  <i class="bi bi-cart-x display-5 d-block mb-2"></i>' +
        "  Giỏ hàng của bạn đang trống.<br>" +
        '  <a href="products.html" class="btn btn-sm btn-outline-primary mt-3">Quay lại chọn món</a>' +
        "</div>";
    }
    var submitBtn = document.querySelector(
      '#checkout-form button[type="submit"]',
    );
    if (submitBtn) submitBtn.disabled = true;
    return;
  }

  // 2. THUẬT TOÁN TÍNH TỔNG TIỀN VÀ RENDER ĐƠN HÀNG F&B (VÒNG LẶP FOR)
  var subtotal = 0;
  var html = "";

  for (var i = 0; i < cart.length; i++) {
    var item = cart[i];
    var itemTotal = item.price * item.quantity;
    subtotal += itemTotal;

    var optionsDesc =
      "Size " +
      (item.size || "M") +
      ", " +
      (item.sugar || "100%") +
      " đường, " +
      (item.ice || "100%") +
      " đá";
    var toppingDesc =
      item.toppings && item.toppings.length > 0
        ? "+ " + item.toppings.join(", ")
        : "";

    html +=
      '<div class="d-flex justify-content-between align-items-start mb-3 border-bottom pb-2">';
    html += "  <div>";
    html +=
      '    <h6 class="fw-bold mb-1 text-dark" style="font-size: 0.95rem;">' +
      item.name +
      "</h6>";
    html += '    <p class="text-muted small mb-0">' + optionsDesc + "</p>";
    if (toppingDesc !== "") {
      html +=
        '  <p class="text-primary small mb-0 fw-medium">' +
        toppingDesc +
        "</p>";
    }
    if (item.note && item.note.trim() !== "") {
      html +=
        '  <p class="text-secondary small mb-0 fst-italic">Ghi chú: ' +
        item.note +
        "</p>";
    }
    html +=
      '    <span class="text-secondary small fw-semibold">Số lượng: x' +
      item.quantity +
      "</span>";
    html += "  </div>";
    html +=
      '  <span class="fw-bold text-dark">' +
      itemTotal.toLocaleString("vi-VN") +
      "₫</span>";
    html += "</div>";
  }

  if (cartItemsContainer) {
    cartItemsContainer.innerHTML = html;
  }

  var shippingFee = 15000;
  var grandTotal = subtotal + shippingFee;

  if (checkoutSubtotal)
    checkoutSubtotal.textContent = subtotal.toLocaleString("vi-VN") + "₫";
  if (checkoutShipping)
    checkoutShipping.textContent = shippingFee.toLocaleString("vi-VN") + "₫";
  if (checkoutTotal)
    checkoutTotal.textContent = grandTotal.toLocaleString("vi-VN") + "₫";

  // Hàm hỗ trợ hiển thị lỗi Validation
  function showError(inputId, errorId, message) {
    var input = document.getElementById(inputId);
    var error = document.getElementById(errorId);
    if (input) input.classList.add("is-invalid");
    if (error) {
      error.textContent = message;
      error.style.display = "block";
    }
  }

  function clearErrors() {
    var invalidInputs = document.querySelectorAll(".is-invalid");
    for (var j = 0; j < invalidInputs.length; j++) {
      invalidInputs[j].classList.remove("is-invalid");
    }
    var errorMessages = document.querySelectorAll(".invalid-feedback");
    for (var k = 0; k < errorMessages.length; k++) {
      errorMessages[k].style.display = "none";
    }
  }

  // 3. SỰ KIỆN NÚT LỆNH: SUBMIT FORM THANH TOÁN VÀ VALIDATION DỮ LIỆU
  var checkoutForm = document.getElementById("checkout-form");
  if (checkoutForm) {
    checkoutForm.addEventListener("submit", function (e) {
      e.preventDefault();
      clearErrors();

      var nameInput = document.getElementById("checkout-name");
      var phoneInput = document.getElementById("checkout-phone");
      var emailInput = document.getElementById("checkout-email");
      var addressInput = document.getElementById("checkout-address");

      var name = nameInput ? nameInput.value.trim() : "";
      var phone = phoneInput ? phoneInput.value.trim() : "";
      var email = emailInput ? emailInput.value.trim() : "";
      var address = addressInput ? addressInput.value.trim() : "";

      var hasError = false;

      // Kiểm tra họ tên
      if (name === "" || name.length < 3) {
        showError(
          "checkout-name",
          "error-name",
          "Vui lòng nhập họ và tên hợp lệ (tối thiểu 3 ký tự)",
        );
        hasError = true;
      }

      // Kiểm tra số điện thoại (phải là số, tối thiểu 10 ký tự)
      var phoneRegex = /^[0-9]{10,11}$/;
      if (!phoneRegex.test(phone.replace(/\s+/g, ""))) {
        showError(
          "checkout-phone",
          "error-phone",
          "Số điện thoại không hợp lệ (cần từ 10 đến 11 số)",
        );
        hasError = true;
      }

      // Kiểm tra email
      if (
        email === "" ||
        email.indexOf("@") === -1 ||
        email.indexOf(".") === -1
      ) {
        showError(
          "checkout-email",
          "error-email",
          "Địa chỉ email không đúng định dạng",
        );
        hasError = true;
      }

      // Kiểm tra địa chỉ giao hàng
      if (address === "" || address.length < 6) {
        showError(
          "checkout-address",
          "error-address",
          "Vui lòng nhập đầy đủ địa chỉ nhận trà sữa (số nhà, tên đường, phường/xã)",
        );
        hasError = true;
      }

      if (hasError) return;

      // 4. LƯU ĐƠN HÀNG VÀO LỊCH SỬ (ORDER HISTORY TRONG LOCALSTORAGE)
      var newOrder = {
        orderId: "SX" + Math.floor(100000 + Math.random() * 900000),
        orderDate: new Date().toLocaleString("vi-VN"),
        customer: {
          name: name,
          phone: phone,
          email: email,
          address: address,
        },
        items: cart,
        subtotal: subtotal,
        shippingFee: shippingFee,
        totalAmount: grandTotal,
        status: "Đang pha chế",
      };

      var existingOrders = [];
      var storedOrders = localStorage.getItem("orderHistory");
      if (storedOrders) {
        try {
          existingOrders = JSON.parse(storedOrders);
        } catch (err) {
          existingOrders = [];
        }
      }
      existingOrders.unshift(newOrder);
      localStorage.setItem("orderHistory", JSON.stringify(existingOrders));

      // Xóa giỏ hàng sau khi đặt thành công
      localStorage.removeItem("cart");

      // Thông báo xác nhận đặt trà sữa thành công
      alert(
        "Đặt hàng thành công! Mã đơn của bạn là: " +
          newOrder.orderId +
          ".\nTIỆM TRÀ SỮA đang tiến hành pha chế và giao ngay cho bạn!",
      );

      // Đọc lời cảm ơn bằng giọng nói nếu trình duyệt hỗ trợ
      if ("speechSynthesis" in window) {
        var msg = new SpeechSynthesisUtterance(
          "Cảm ơn bạn đã đặt trà sữa tại Tiệm Trà Sữa",
        );
        msg.lang = "vi-VN";
        msg.onend = function () {
          window.location.href = "profile.html";
        };
        window.speechSynthesis.speak(msg);
      } else {
        window.location.href = "profile.html";
      }
    });
  }
});
