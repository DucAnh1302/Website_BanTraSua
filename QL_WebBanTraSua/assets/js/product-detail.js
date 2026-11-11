// ==========================================================
// TIỆM TRÀ SỮA - CHI TIẾT MÓN (product-detail.js)
// Tải món từ JSON, Tính giá thời gian thực (+Size, +Topping), Thêm giỏ hàng
// ==========================================================

var currentProduct = null;
var currentQuantity = 1;

document.addEventListener("DOMContentLoaded", function () {
  var urlParams = new URLSearchParams(window.location.search);
  var productId = parseInt(urlParams.get("id")) || 1; // Mặc định món số 1 nếu không truyền id

  // 1. DÙNG FETCH API ĐỌC DỮ LIỆU TỪ FILE JSON
  fetch("../../data/products.json").catch(function() { return fetch("../data/products.json"); })
    .then(function (response) {
      if (!response.ok) throw new Error("Không thể nạp dữ liệu món nước.");
      return response.json();
    })
    .then(function (productsList) {
      // 2. THUẬT TOÁN TÌM KIẾM THEO ID (VÒNG LẶP FOR THEO CHUẨN ĐỒ ÁN)
      for (var i = 0; i < productsList.length; i++) {
        if (productsList[i].id === productId) {
          currentProduct = productsList[i];
          break;
        }
      }

      // Nếu không tìm thấy, lấy món đầu tiên
      if (!currentProduct && productsList.length > 0) {
        currentProduct = productsList[0];
      }

      if (currentProduct) {
        renderProductDetailView(currentProduct);
        setupProductDetailInteractions();
      } else {
        showProductNotFound();
      }
    })
    .catch(function (error) {
      console.error("Lỗi:", error);
      showProductNotFound();
    });
});

// Hàm hiển thị khi không tìm thấy món
function showProductNotFound() {
  var container = document.getElementById("product-detail-container");
  if (container) {
    container.innerHTML =
      '<div class="col-12 text-center py-5">' +
      '  <i class="bi bi-exclamation-circle display-3 text-warning mb-3 d-block"></i>' +
      '  <h3 class="fw-bold text-dark mb-3">Món nước không tồn tại hoặc đã tạm dừng phục vụ</h3>' +
      '  <p class="text-muted mb-4">Vui lòng quay lại thực đơn để chọn các thức uống hấp dẫn khác của Tiệm Trà TIỆM TRÀ SỮA.</p>' +
      '  <a href="products.html" class="btn btn-primary rounded-pill px-4 py-2 fw-bold">Xem Thực Đơn</a>' +
      "</div>";
  }
}

// 3. RENDER NỘI DUNG CHI TIẾT MÓN TRÀ SỮA LÊN DOM
function renderProductDetailView(product) {
  // Cập nhật Breadcrumb
  var breadcrumbs = document.querySelectorAll(
    ".breadcrumb-item.active, #product-breadcrumb",
  );
  for (var b = 0; b < breadcrumbs.length; b++) {
    breadcrumbs[b].textContent = product.name;
  }
  document.title = product.name + " - TIỆM TRÀ SỮA";

  var container = document.getElementById("product-detail-container");
  if (!container) return;

  var basePriceFormatted =
    typeof formatPrice === "function"
      ? formatPrice(product.price)
      : product.price.toLocaleString("vi-VN") + "₫";

  container.innerHTML = `
    <!-- Cột trái: Ảnh món nước -->
    <div class="col-lg-5">
      <div class="position-relative overflow-hidden rounded-4 shadow-sm bg-white p-2 border border-light-subtle">
        <img
          src="${product.image}"
          class="img-fluid rounded-4 w-100 object-fit-cover"
          alt="${product.name}"
          style="aspect-ratio: 4/5; max-height: 520px;"
          onerror="this.src='https://images.unsplash.com/photo-1558857563-b3710344c8b0?q=80&w=600&auto=format&fit=crop'"
        />
        ${product.isBestSeller ? '<span class="position-absolute top-0 start-0 m-4 badge bg-danger py-2 px-3 rounded-pill fw-bold text-uppercase shadow-sm">Bán chạy</span>' : ""}
      </div>
    </div>

    <!-- Cột phải: Thông tin & Tùy chỉnh F&B -->
    <div class="col-lg-7">
      <h2 class="fw-bolder mb-2 text-dark">${product.name}</h2>
      <p class="text-muted mb-3 fs-6 lh-base">${product.description}</p>
      <div class="d-flex align-items-baseline gap-3 mb-4">
        <h3 class="text-primary fw-bolder mb-0 display-6" id="product-base-price">${basePriceFormatted}</h3>
        <span class="badge bg-light text-secondary border px-3 py-2 rounded-pill">Kho: ${product.stock || 100} phần</span>
      </div>

      <form id="product-form" class="bg-white p-4 rounded-4 shadow-sm border border-light-subtle">
        <!-- 1. Chọn Size -->
        <div class="mb-4">
          <h6 class="fw-bold mb-3 text-dark">
            <i class="bi bi-cup-straw text-primary me-2"></i>1. Chọn Kích Cỡ (Size) <span class="text-danger">*</span>
          </h6>
          <div class="d-flex flex-wrap gap-3">
            <input type="radio" class="btn-check option-trigger" name="size" id="sizeM" value="M" checked>
            <label class="btn btn-outline-secondary option-pill" for="sizeM">Size M (+0₫)</label>

            <input type="radio" class="btn-check option-trigger" name="size" id="sizeL" value="L">
            <label class="btn btn-outline-secondary option-pill" for="sizeL">Size L (+10.000₫)</label>
          </div>
        </div>

        <div class="row">
          <!-- 2. Lượng Đường -->
          <div class="col-md-6 mb-4">
            <h6 class="fw-bold mb-3 text-dark">
              <i class="bi bi-droplet-half text-primary me-2"></i>2. Lượng Đường <span class="text-danger">*</span>
            </h6>
            <div class="d-flex flex-wrap gap-2">
              <input type="radio" class="btn-check option-trigger" name="sugar" id="sugar100" value="100%" checked>
              <label class="btn btn-outline-secondary option-pill" for="sugar100">100%</label>

              <input type="radio" class="btn-check option-trigger" name="sugar" id="sugar70" value="70%">
              <label class="btn btn-outline-secondary option-pill" for="sugar70">70%</label>

              <input type="radio" class="btn-check option-trigger" name="sugar" id="sugar50" value="50%">
              <label class="btn btn-outline-secondary option-pill" for="sugar50">50%</label>

              <input type="radio" class="btn-check option-trigger" name="sugar" id="sugar30" value="30%">
              <label class="btn btn-outline-secondary option-pill" for="sugar30">30%</label>

              <input type="radio" class="btn-check option-trigger" name="sugar" id="sugar0" value="0%">
              <label class="btn btn-outline-secondary option-pill" for="sugar0">0%</label>
            </div>
          </div>

          <!-- 3. Lượng Đá -->
          <div class="col-md-6 mb-4">
            <h6 class="fw-bold mb-3 text-dark">
              <i class="bi bi-snow text-primary me-2"></i>3. Lượng Đá <span class="text-danger">*</span>
            </h6>
            <div class="d-flex flex-wrap gap-2">
              <input type="radio" class="btn-check option-trigger" name="ice" id="ice100" value="100%" checked>
              <label class="btn btn-outline-secondary option-pill" for="ice100">100%</label>

              <input type="radio" class="btn-check option-trigger" name="ice" id="ice50" value="50%">
              <label class="btn btn-outline-secondary option-pill" for="ice50">50%</label>

              <input type="radio" class="btn-check option-trigger" name="ice" id="iceRieng" value="Đá riêng">
              <label class="btn btn-outline-secondary option-pill" for="iceRieng">Đá riêng</label>

              <input type="radio" class="btn-check option-trigger" name="ice" id="ice0" value="Không đá">
              <label class="btn btn-outline-secondary option-pill" for="ice0">Không đá</label>
            </div>
          </div>
        </div>

        <!-- 4. Topping Thêm -->
        <div class="mb-4">
          <h6 class="fw-bold mb-3 text-dark">
            <i class="bi bi-stars text-primary me-2"></i>4. Thêm Topping (Tùy chọn)
          </h6>
          <div class="row g-2">
            <div class="col-sm-6">
              <div class="form-check topping-card d-flex justify-content-between align-items-center">
                <div>
                  <input class="form-check-input option-trigger" type="checkbox" name="topping" value="Trân Châu Trắng" id="topTCTrang" data-price="10000">
                  <label class="form-check-label fw-medium ms-1" for="topTCTrang">Trân Châu Trắng</label>
                </div>
                <span class="text-primary fw-bold">+10.000₫</span>
              </div>
            </div>
            <div class="col-sm-6">
              <div class="form-check topping-card d-flex justify-content-between align-items-center">
                <div>
                  <input class="form-check-input option-trigger" type="checkbox" name="topping" value="Pudding Trứng" id="topPudding" data-price="10000">
                  <label class="form-check-label fw-medium ms-1" for="topPudding">Pudding Trứng</label>
                </div>
                <span class="text-primary fw-bold">+10.000₫</span>
              </div>
            </div>
            <div class="col-sm-6">
              <div class="form-check topping-card d-flex justify-content-between align-items-center">
                <div>
                  <input class="form-check-input option-trigger" type="checkbox" name="topping" value="Lớp Kem Cheese Khấu Đảo" id="topKemCheese" data-price="15000">
                  <label class="form-check-label fw-medium ms-1" for="topKemCheese">Kem Cheese Phô Mai</label>
                </div>
                <span class="text-primary fw-bold">+15.000₫</span>
              </div>
            </div>
            <div class="col-sm-6">
              <div class="form-check topping-card d-flex justify-content-between align-items-center">
                <div>
                  <input class="form-check-input option-trigger" type="checkbox" name="topping" value="Sương Sáo Bạc Hà" id="topSuongSao" data-price="10000">
                  <label class="form-check-label fw-medium ms-1" for="topSuongSao">Sương Sáo Bạc Hà</label>
                </div>
                <span class="text-primary fw-bold">+10.000₫</span>
              </div>
            </div>
          </div>
        </div>

        <!-- 5. Ghi chú món -->
        <div class="mb-4">
          <label for="order-note" class="form-label text-dark fw-semibold small">Ghi chú riêng cho Quán (nếu có):</label>
          <input type="text" id="order-note" class="form-control rounded-3 bg-light border-0 shadow-none py-2" placeholder="Ví dụ: Cho ít ngọt, giao gấp...">
        </div>

        <!-- 6. Nút Tăng Giảm Số Lượng và Nút Thao Tác -->
        <div class="d-flex flex-column flex-md-row gap-3 align-items-center pt-3 border-top border-light-subtle">
          <div class="input-group" style="width: 140px;">
            <button class="btn btn-outline-secondary px-3 fw-bold" type="button" id="btn-qty-minus">-</button>
            <input type="text" class="form-control text-center fw-bold bg-white" value="1" id="product-qty" readonly>
            <button class="btn btn-outline-secondary px-3 fw-bold" type="button" id="btn-qty-plus">+</button>
          </div>
          <button type="button" class="btn btn-outline-dark btn-lg rounded-pill fw-bold text-uppercase px-4 shadow-sm" id="btn-add-to-cart">
            <i class="bi bi-bag-plus me-1"></i> Thêm vào giỏ
          </button>
          <button type="button" class="btn btn-primary btn-lg rounded-pill flex-grow-1 fw-bold text-uppercase shadow-sm" id="btn-buy-now">
            Mua Ngay - <span id="dynamic-price">${basePriceFormatted}</span>
          </button>
        </div>
      </form>
    </div>
  `;
}

// 4. SỰ KIỆN TƯƠNG TÁC: TÍNH TOÁN GIÁ ĐỘNG THỜI GIAN THỰC & THÊM GIỎ HÀNG
function setupProductDetailInteractions() {
  currentQuantity = 1;

  // Lắng nghe thay đổi trên tất cả radio button và checkbox
  var triggers = document.querySelectorAll(".option-trigger");
  for (var i = 0; i < triggers.length; i++) {
    triggers[i].addEventListener("change", calculateLivePrice);
  }

  // Tăng số lượng
  var btnPlus = document.getElementById("btn-qty-plus");
  if (btnPlus) {
    btnPlus.addEventListener("click", function () {
      currentQuantity++;
      var qtyInput = document.getElementById("product-qty");
      if (qtyInput) qtyInput.value = currentQuantity;
      calculateLivePrice();
    });
  }

  // Giảm số lượng
  var btnMinus = document.getElementById("btn-qty-minus");
  if (btnMinus) {
    btnMinus.addEventListener("click", function () {
      if (currentQuantity > 1) {
        currentQuantity--;
        var qtyInput = document.getElementById("product-qty");
        if (qtyInput) qtyInput.value = currentQuantity;
        calculateLivePrice();
      }
    });
  }

  // Nút Thêm vào giỏ
  var btnAddToCart = document.getElementById("btn-add-to-cart");
  if (btnAddToCart) {
    btnAddToCart.addEventListener("click", function () {
      executeAddOrder(false);
    });
  }

  // Nút Mua ngay
  var btnBuyNow = document.getElementById("btn-buy-now");
  if (btnBuyNow) {
    btnBuyNow.addEventListener("click", function () {
      executeAddOrder(true);
    });
  }

  // Tính giá lần đầu
  calculateLivePrice();
}

// Hàm tính giá tiền động theo thời gian thực (DOM + Toán tử)
function calculateLivePrice() {
  if (!currentProduct) return;

  var basePrice = currentProduct.price || 0;

  // 1. Phụ thu size
  var sizeSurcharge = 0;
  var selectedSizeEl = document.querySelector('input[name="size"]:checked');
  if (selectedSizeEl && selectedSizeEl.value === "L") {
    sizeSurcharge = 10000;
  }

  // 2. Phụ thu topping
  var toppingSurcharge = 0;
  var checkedToppings = document.querySelectorAll(
    'input[name="topping"]:checked',
  );
  for (var i = 0; i < checkedToppings.length; i++) {
    var price =
      parseInt(checkedToppings[i].getAttribute("data-price")) || 10000;
    toppingSurcharge += price;
  }

  // Đơn giá 1 ly
  var unitPrice = basePrice + sizeSurcharge + toppingSurcharge;
  // Tổng tiền theo số lượng
  var totalPrice = unitPrice * currentQuantity;

  var dynamicPriceEl = document.getElementById("dynamic-price");
  if (dynamicPriceEl) {
    dynamicPriceEl.textContent =
      typeof formatPrice === "function"
        ? formatPrice(totalPrice)
        : totalPrice.toLocaleString("vi-VN") + "₫";
  }
}

// Thu thập thông tin món và đẩy vào giỏ hàng
function executeAddOrder(isBuyNow) {
  if (!currentProduct) return;

  // Lấy Size
  var size = "M";
  var sizeEl = document.querySelector('input[name="size"]:checked');
  if (sizeEl) size = sizeEl.value;

  // Lấy Lượng Đường
  var sugar = "100%";
  var sugarEl = document.querySelector('input[name="sugar"]:checked');
  if (sugarEl) sugar = sugarEl.value;

  // Lấy Lượng Đá
  var ice = "100%";
  var iceEl = document.querySelector('input[name="ice"]:checked');
  if (iceEl) ice = iceEl.value;

  // Lấy danh sách Topping đã chọn
  var toppings = [];
  var checkedToppings = document.querySelectorAll(
    'input[name="topping"]:checked',
  );
  for (var i = 0; i < checkedToppings.length; i++) {
    toppings.push(checkedToppings[i].value);
  }

  // Lấy ghi chú
  var noteInput = document.getElementById("order-note");
  var note = noteInput ? noteInput.value.trim() : "";

  // Gọi hàm addToCart toàn cục trong main.js
  if (typeof addToCart === "function") {
    addToCart(
      currentProduct,
      currentQuantity,
      size,
      sugar,
      ice,
      toppings,
      note,
      isBuyNow,
    );
  }

  if (isBuyNow) {
    window.location.href = "checkout.html";
  }
}
