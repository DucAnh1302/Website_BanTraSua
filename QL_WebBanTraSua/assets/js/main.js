// ==========================================================
// TIỆM TRÀ SỮA - SCRIPT CHÍNH (main.js)
// Bao gồm: DOM, Sự kiện, Quản lý Trạng thái & Giỏ hàng F&B
// ==========================================================

// 1. DOM CƠ BẢN VÀ HIỆU ỨNG CUỘN MƯỢT
var anchorLinks = document.querySelectorAll('a[href^="#"]');
for (var a = 0; a < anchorLinks.length; a++) {
  anchorLinks[a].addEventListener("click", function (e) {
    var href = this.getAttribute("href");
    if (href !== "#" && href.length > 1) {
      var target = document.querySelector(href);
      if (target) {
        e.preventDefault();
        target.scrollIntoView({ behavior: "smooth", block: "start" });
      }
    }
  });
}

// 2. SỰ KIỆN NÚT LỆNH: FORM ĐĂNG KÝ NHẬN BẢN TIN (NEWSLETTER)
var newsletterForm = document.querySelector(".newsletter-form");
if (newsletterForm) {
  newsletterForm.addEventListener("submit", function (e) {
    e.preventDefault();
    var emailInput = this.querySelector('input[type="email"]');
    if (emailInput && emailInput.value.trim() !== "") {
      alert(
        "Cảm ơn bạn! Email " +
          emailInput.value.trim() +
          " đã đăng ký nhận ưu đãi thành công từ Tiệm Trà TIỆM TRÀ SỮA.",
      );
      this.reset();
    }
  });
}

// 3. HIỆU ỨNG THANH HEADER KHI CUỘN TRANG
window.addEventListener("scroll", function () {
  var header = document.querySelector(".main-header, .header");
  if (header) {
    if (window.scrollY > 80) {
      header.style.boxShadow = "0 4px 15px rgba(0, 0, 0, 0.25)";
    } else {
      header.style.boxShadow = "none";
    }
  }
});

// 4. QUẢN LÝ TRẠNG THÁI NGƯỜI DÙNG ĐĂNG NHẬP / ĐĂNG XUẤT (LOCALSTORAGE)
function checkUserStatus() {
  var currentUserData = localStorage.getItem("currentUser");
  var userMenuDropdown = document.getElementById("user-menu-dropdown");

  if (currentUserData && userMenuDropdown) {
    var user = JSON.parse(currentUserData);

    // Giao diện khi người dùng đã đăng nhập thành công
    var menuHtml = "";
    menuHtml +=
      '<li><a class="dropdown-item py-2 fw-bold text-white" href="profile.html" id="nav-user-greeting"></a></li>';
    if (user.role === "admin") {
      menuHtml +=
        '<li><a class="dropdown-item py-2 text-warning fw-bold" href="admin.html"><i class="bi bi-speedometer2 me-1"></i> Trang Quản Trị</a></li>';
    }
    menuHtml +=
      '<li><a class="dropdown-item py-2 text-secondary" href="profile.html">Hồ sơ cá nhân</a></li>';
    menuHtml += '<li><hr class="dropdown-divider border-secondary"></li>';
    menuHtml +=
      '<li><a class="dropdown-item py-2 text-danger" href="#" id="btn-logout">Đăng xuất</a></li>';

    userMenuDropdown.innerHTML = menuHtml;

    // Chống lỗ hổng XSS bằng gán textContent
    var greetingEl = document.getElementById("nav-user-greeting");
    if (greetingEl) {
      greetingEl.textContent =
        "Xin chào, " + (user.full_name || user.email || "Bạn") + "!";
    }

    // Sự kiện Đăng xuất
    var logoutBtn = document.getElementById("btn-logout");
    if (logoutBtn) {
      logoutBtn.addEventListener("click", function (e) {
        e.preventDefault();
        if (confirm("Bạn có chắc chắn muốn đăng xuất tài khoản?")) {
          localStorage.removeItem("currentUser");
          alert("Đã đăng xuất thành công!");
          checkUserStatus();
          window.location.reload();
        }
      });
    }
  } else if (userMenuDropdown) {
    // Giao diện mặc định khi chưa đăng nhập
    userMenuDropdown.innerHTML =
      '<li><a class="dropdown-item py-2 text-white" href="login.html"><i class="bi bi-box-arrow-in-right me-1"></i> Đăng nhập</a></li>' +
      '<li><a class="dropdown-item py-2 text-secondary" href="register.html"><i class="bi bi-person-plus me-1"></i> Đăng ký</a></li>';
  }
}

// Khởi chạy kiểm tra người dùng khi DOM sẵn sàng
document.addEventListener("DOMContentLoaded", checkUserStatus);

// ==========================================================
// 5. MÔ HÌNH GIỎ HÀNG F&B ĐẶC THÙ CHO QUÁN TRÀ SỮA (CART LOGIC)
// ==========================================================
var cart = [];

// Hàm tải giỏ hàng từ LocalStorage
function loadCart() {
  var savedCart = localStorage.getItem("cart");
  if (savedCart) {
    try {
      cart = JSON.parse(savedCart);
    } catch (e) {
      cart = [];
    }
  } else {
    cart = [];
  }
  updateCart();
}

// Hàm lưu giỏ hàng vào LocalStorage
function saveCart() {
  localStorage.setItem("cart", JSON.stringify(cart));
}

// Hàm định dạng tiền tệ Việt Nam (VND)
function formatPrice(price) {
  if (typeof price !== "number") price = Number(price) || 0;
  return price.toLocaleString("vi-VN") + "₫";
}

/**
 * Thêm thức uống vào giỏ hàng với đầy đủ cấu hình F&B:
 * @param {Object} product - Thông tin món (id, name, price, image)
 * @param {Number} quantity - Số lượng (mặc định: 1)
 * @param {String} size - Size "M" hoặc "L" (+10.000đ)
 * @param {String} sugar - Lượng đường ("100%", "50%", "30%", "0%")
 * @param {String} ice - Lượng đá ("100%", "50%", "Đá riêng", "Không đá")
 * @param {Array} toppings - Mảng các topping chọn thêm (["Pudding Trứng", "Trân Châu Trắng"])
 * @param {String} note - Ghi chú riêng
 * @param {Boolean} skipOffcanvas - Bỏ qua mở sidebar giỏ hàng (dùng khi Mua Ngay)
 */
function addToCart(
  product,
  quantity,
  size,
  sugar,
  ice,
  toppings,
  note,
  skipOffcanvas,
) {
  if (quantity === undefined || quantity <= 0) quantity = 1;
  if (size === undefined) size = "M";
  if (sugar === undefined) sugar = "100%";
  if (ice === undefined) ice = "100%";
  if (toppings === undefined || !Array.isArray(toppings)) toppings = [];
  if (note === undefined) note = "";
  if (skipOffcanvas === undefined) skipOffcanvas = false;

  // Tính phụ thu Size
  var sizeSurcharge = size === "L" ? 10000 : 0;

  // Tính phụ thu Toppings (Chuẩn giá 10k/15k tùy loại)
  var toppingSurcharge = 0;
  for (var t = 0; t < toppings.length; t++) {
    var topName = toppings[t].toLowerCase();
    if (topName.indexOf("cheese") !== -1 || topName.indexOf("kem") !== -1) {
      toppingSurcharge += 15000;
    } else {
      toppingSurcharge += 10000;
    }
  }

  // Đơn giá thực tế của một ly sau khi cộng tùy chọn
  var unitPrice = (product.price || 0) + sizeSurcharge + toppingSurcharge;

  // Thuật toán kiểm tra sản phẩm trùng cấu hình trong giỏ hàng (Dùng vòng lặp for)
  var existingItem = null;
  var toppingsKey = toppings.slice().sort().join(",");

  for (var i = 0; i < cart.length; i++) {
    var item = cart[i];
    var itemTopKey = (item.toppings || []).slice().sort().join(",");
    if (
      item.id === product.id &&
      item.size === size &&
      item.sugar === sugar &&
      item.ice === ice &&
      itemTopKey === toppingsKey
    ) {
      existingItem = item;
      break;
    }
  }

  if (existingItem) {
    existingItem.quantity += quantity;
  } else {
    cart.push({
      id: product.id,
      name: product.name,
      basePrice: product.price,
      price: unitPrice,
      quantity: quantity,
      size: size,
      sugar: sugar,
      ice: ice,
      toppings: toppings,
      note: note,
      image: product.image || "https://via.placeholder.com/150",
    });
  }

  saveCart();
  updateCart();

  // Tự động mở Offcanvas Giỏ Hàng
  if (!skipOffcanvas) {
    var cartOffcanvasEl = document.getElementById("cartOffcanvas");
    if (cartOffcanvasEl && typeof bootstrap !== "undefined") {
      var offcanvasInstance =
        bootstrap.Offcanvas.getOrCreateInstance(cartOffcanvasEl);
      offcanvasInstance.show();
    }
  }
}

// Xóa một món theo chỉ số index trong mảng giỏ hàng
function removeFromCart(index) {
  if (index >= 0 && index < cart.length) {
    cart.splice(index, 1);
    saveCart();
    updateCart();
  }
}

// Xóa toàn bộ giỏ hàng
function clearCart() {
  if (cart.length === 0) return;
  if (confirm("Bạn có chắc muốn xóa toàn bộ giỏ hàng?")) {
    cart = [];
    saveCart();
    updateCart();
  }
}

// Cập nhật giao diện giỏ hàng thời gian thực (DOM)
function updateCart() {
  var cartItems = document.getElementById("cart-items");
  var cartTotal = document.getElementById("cart-total");
  var checkoutBtn = document.getElementById("checkout-btn");
  var cartBadges = document.querySelectorAll(".cart-count-badge");

  // 1. Thuật toán tính tổng số lượng ly trà sữa trong giỏ (Dùng vòng lặp for)
  var totalItems = 0;
  for (var i = 0; i < cart.length; i++) {
    totalItems += cart[i].quantity;
  }

  // 2. Cập nhật số lượng trên các badge giỏ hàng
  for (var j = 0; j < cartBadges.length; j++) {
    cartBadges[j].textContent = totalItems;
  }

  // 3. Xử lý trường hợp giỏ hàng trống
  if (cart.length === 0) {
    if (cartItems) {
      cartItems.innerHTML =
        '<div class="text-center py-5 text-secondary">' +
        '  <i class="bi bi-cup-straw display-4 d-block mb-3 text-muted"></i>' +
        '  <p class="mb-0">Giỏ hàng của bạn đang trống.</p>' +
        '  <small class="text-muted">Hãy chọn cho mình một ly trà sữa thơm ngon nhé!</small>' +
        "</div>";
    }
    if (cartTotal) cartTotal.textContent = "0₫";
    if (checkoutBtn) checkoutBtn.disabled = true;
    return;
  }

  if (checkoutBtn) checkoutBtn.disabled = false;

  // 4. Thuật toán tính tổng tiền và sinh HTML giỏ hàng F&B (Vòng lặp for)
  var total = 0;
  var cartHtml = "";

  for (var k = 0; k < cart.length; k++) {
    var item = cart[k];
    var itemTotal = item.price * item.quantity;
    total += itemTotal;

    var toppingStr =
      item.toppings && item.toppings.length > 0
        ? item.toppings.join(", ")
        : "Không topping";
    var optionsDesc =
      "Size " +
      (item.size || "M") +
      " • " +
      (item.sugar || "100%") +
      " đường • " +
      (item.ice || "100%") +
      " đá";

    cartHtml +=
      '<div class="offcanvas-cart-item d-flex align-items-center justify-content-between p-3 border-bottom">';
    cartHtml +=
      '  <div class="d-flex align-items-center gap-3" style="max-width: 70%;">';
    cartHtml +=
      '    <img src="' +
      item.image +
      '" alt="' +
      item.name +
      '" class="rounded-3 object-fit-cover shadow-sm" style="width: 50px; height: 50px;" onerror="this.src=\'https://via.placeholder.com/100\'">';
    cartHtml += "    <div>";
    cartHtml +=
      '      <h6 class="mb-1 fw-bold text-dark text-truncate" style="max-width: 180px; font-size: 0.95rem;">' +
      item.name +
      "</h6>";
    cartHtml +=
      '      <div class="text-muted" style="font-size: 0.78rem;">' +
      optionsDesc +
      "</div>";
    if (item.toppings && item.toppings.length > 0) {
      cartHtml +=
        '    <div class="text-primary" style="font-size: 0.75rem;">+ ' +
        toppingStr +
        "</div>";
    }
    cartHtml +=
      '      <div class="fw-semibold text-secondary mt-1" style="font-size: 0.8rem;">SL: ' +
      item.quantity +
      " &times; " +
      formatPrice(item.price) +
      "</div>";
    cartHtml += "    </div>";
    cartHtml += "  </div>";
    cartHtml += '  <div class="text-end">';
    cartHtml +=
      '    <div class="fw-bold text-primary mb-1" style="font-size: 0.95rem;">' +
      formatPrice(itemTotal) +
      "</div>";
    cartHtml +=
      '    <button type="button" class="btn btn-sm btn-link text-danger text-decoration-none p-0 cart-remove-btn" data-cart-index="' +
      k +
      '">';
    cartHtml += '      <i class="bi bi-trash3 me-1"></i>Xóa';
    cartHtml += "    </button>";
    cartHtml += "  </div>";
    cartHtml += "</div>";
  }

  if (cartItems) cartItems.innerHTML = cartHtml;
  if (cartTotal) cartTotal.textContent = formatPrice(total);

  // 5. Gắn sự kiện Xóa Món cho các nút lệnh
  var removeBtns = document.querySelectorAll(".cart-remove-btn");
  for (var m = 0; m < removeBtns.length; m++) {
    removeBtns[m].addEventListener("click", function () {
      var index = parseInt(this.getAttribute("data-cart-index"));
      removeFromCart(index);
    });
  }
}

// Khởi chạy logic giỏ hàng và gắn sự kiện
document.addEventListener("DOMContentLoaded", function () {
  loadCart();

  // Nút Chuyển đến Thanh Toán trong Offcanvas
  var checkoutBtn = document.getElementById("checkout-btn");
  if (checkoutBtn) {
    checkoutBtn.addEventListener("click", function () {
      if (cart.length === 0) {
        alert(
          "Giỏ hàng của bạn đang trống. Vui lòng chọn món trước khi thanh toán!",
        );
        return;
      }
      var currentUser = localStorage.getItem("currentUser");
      var isCustomer = window.location.pathname.indexOf("/customer/") !== -1;
      if (!currentUser) {
        if (
          confirm(
            "Vui lòng đăng nhập để tiến hành đặt món và tích điểm thành viên. Đến trang đăng nhập ngay?",
          )
        ) {
          window.location.href = isCustomer
            ? "../auth/login.html"
            : "auth/login.html";
        }
        return;
      }
      window.location.href = isCustomer
        ? "checkout.html"
        : "customer/checkout.html";
    });
  }

  // Nút Xóa toàn bộ giỏ hàng
  var clearCartBtn = document.getElementById("clear-cart-btn");
  if (clearCartBtn) {
    clearCartBtn.addEventListener("click", clearCart);
  }

  // Khởi tạo các tính năng cao cấp lấy cảm hứng từ WAO Tea Coffee
  initFloatingWidget();
  initLiveSearch();
});

// ==========================================================
// 8. TÍNH NĂNG 1: CỤM NÚT TƯƠNG TÁC NỔI & BACK-TO-TOP (WAO STYLE)
// ==========================================================
function initFloatingWidget() {
  if (document.querySelector(".floating-actions-wrap")) return;

  var wrap = document.createElement("div");
  wrap.className = "floating-actions-wrap";
  wrap.innerHTML =
    '<a href="tel:19001234" class="btn-floating btn-floating-hotline" data-tooltip="Hotline: 1900 1234" aria-label="Hotline đặt hàng">' +
    '  <i class="bi bi-telephone-fill"></i>' +
    "</a>" +
    '<a href="https://zalo.me" target="_blank" rel="noopener noreferrer" class="btn-floating btn-floating-zalo" data-tooltip="Tư vấn Zalo OA" aria-label="Zalo OA">' +
    '  <i class="bi bi-chat-dots-fill"></i>' +
    "</a>" +
    '<button type="button" class="btn-floating btn-floating-top" id="btn-back-to-top" data-tooltip="Về đầu trang" aria-label="Cuộn lên đầu trang">' +
    '  <i class="bi bi-chevron-up"></i>' +
    "</button>";

  document.body.appendChild(wrap);

  var backToTopBtn = document.getElementById("btn-back-to-top");
  if (backToTopBtn) {
    window.addEventListener("scroll", function () {
      if (window.scrollY > 300) {
        backToTopBtn.classList.add("show");
      } else {
        backToTopBtn.classList.remove("show");
      }
    });

    backToTopBtn.addEventListener("click", function () {
      window.scrollTo({ top: 0, behavior: "smooth" });
    });
  }
}

// ==========================================================
// 9. TÍNH NĂNG 2: TÌM KIẾM TRỰC TIẾP THỜI GIAN THỰC (LIVE SEARCH)
// ==========================================================
function initLiveSearch() {
  var searchModal = document.getElementById("searchModal");
  if (!searchModal) return;

  var searchInput = searchModal.querySelector('input[name="search"]');
  var modalBody = searchModal.querySelector(".modal-body");
  if (!searchInput || !modalBody) return;

  // Tạo container kết quả gợi ý nếu chưa có
  var resultsContainer = document.getElementById("liveSearchResults");
  if (!resultsContainer) {
    resultsContainer = document.createElement("div");
    resultsContainer.id = "liveSearchResults";
    resultsContainer.className = "live-search-results d-none";
    modalBody.appendChild(resultsContainer);
  }

  // Tải trước danh sách món nước
  var allDrinks = [];
  var isSubFolder =
    window.location.pathname.indexOf("/customer/") !== -1 ||
    window.location.pathname.indexOf("/auth/") !== -1;
  var dataPath = isSubFolder
    ? "../../data/products.json"
    : "../data/products.json";
  fetch(dataPath)
    .then(function (res) {
      return res.json();
    })
    .then(function (data) {
      allDrinks = data;
    })
    .catch(function () {
      // Thử đường dẫn dự phòng
      fetch("../data/products.json")
        .then(function (res) {
          return res.json();
        })
        .then(function (data) {
          allDrinks = data;
        })
        .catch(function () {});
    });

  // Lắng nghe sự kiện gõ từ khóa
  searchInput.addEventListener("input", function () {
    var keyword = this.value.trim().toLowerCase();
    if (keyword.length === 0) {
      resultsContainer.classList.add("d-none");
      resultsContainer.innerHTML = "";
      return;
    }

    var matched = allDrinks.filter(function (item) {
      return (
        item.name.toLowerCase().indexOf(keyword) !== -1 ||
        (item.categoryName &&
          item.categoryName.toLowerCase().indexOf(keyword) !== -1)
      );
    });

    if (matched.length === 0) {
      resultsContainer.classList.remove("d-none");
      resultsContainer.innerHTML =
        '<div class="p-3 text-center text-muted small"><i class="bi bi-emoji-frown me-1"></i> Không tìm thấy thức uống nào với từ khóa "' +
        escapeHtml(keyword) +
        '"</div>';
      return;
    }

    // Hiển thị tối đa 5 kết quả đầu tiên
    var html = "";
    var isCustomerPath = window.location.pathname.indexOf("/customer/") !== -1;
    var prefix = isCustomerPath ? "" : "customer/";
    var limit = Math.min(matched.length, 5);
    for (var i = 0; i < limit; i++) {
      var prod = matched[i];
      html +=
        '<a href="' +
        prefix +
        "product-detail.html?id=" +
        prod.id +
        '" class="live-search-item">' +
        '  <img src="' +
        prod.image +
        '" alt="' +
        escapeHtml(prod.name) +
        '" class="live-search-thumb" />' +
        '  <div class="live-search-info">' +
        '    <div class="live-search-name">' +
        escapeHtml(prod.name) +
        "</div>" +
        "    <div>" +
        '      <span class="live-search-price">' +
        formatPrice(prod.price) +
        "</span>" +
        '      <span class="live-search-cat">' +
        escapeHtml(prod.categoryName || "") +
        "</span>" +
        "    </div>" +
        "  </div>" +
        '  <i class="bi bi-chevron-right text-secondary small"></i>' +
        "</a>";
    }

    if (matched.length > 5) {
      html +=
        '<div class="p-2 text-center border-top border-dark">' +
        '  <a href="' +
        prefix +
        "products.html?search=" +
        encodeURIComponent(keyword) +
        '" class="text-primary small fw-bold text-decoration-none">Xem tất cả ' +
        matched.length +
        ' kết quả <i class="bi bi-arrow-right"></i></a>' +
        "</div>";
    }

    resultsContainer.classList.remove("d-none");
    resultsContainer.innerHTML = html;
  });
}
