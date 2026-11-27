// ==========================================================
// TIỆM TRÀ SỮA - DANH SÁCH MÓN NƯỚC (products.js)
// Nạp CSDL JSON, Lọc theo Danh mục, Khoảng giá, Tìm kiếm, Sắp xếp
// ==========================================================

var rawProducts = [];
var currentFilteredProducts = [];
var currentCategoryFilter = "all";
var currentPriceRange = { min: 0, max: 99999999 };
var currentSortMode = "newest";

// Hàm hỗ trợ chuyển đổi tiếng Việt có dấu thành không dấu để tìm kiếm thông minh
function removeVietnameseTones(str) {
  if (!str) return "";
  str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, "a");
  str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, "e");
  str = str.replace(/ì|í|ị|ỉ|ĩ/g, "i");
  str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, "o");
  str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, "u");
  str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, "y");
  str = str.replace(/đ/g, "d");
  str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, "A");
  str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, "E");
  str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, "I");
  str = str.replace(/Ò|Ó|Ọ|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, "O");
  str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, "U");
  str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, "Y");
  str = str.replace(/Đ/g, "D");
  return str.toLowerCase().trim();
}

// 1. DÙNG FETCH API NẠP TOÀN BỘ SẢN PHẨM TỪ FILE JSON
function loadProductsFromDatabase() {
  fetch("../../data/products.json").catch(function() { return fetch("../data/products.json"); })
    .then(function (response) {
      if (!response.ok) throw new Error("Không thể tải danh sách sản phẩm.");
      return response.json();
    })
    .then(function (data) {
      rawProducts = data;
      applyAllFiltersAndRender();
    })
    .catch(function (error) {
      console.error("Lỗi nạp sản phẩm:", error);
      handleProductLoadError();
    });
}

// 2. THUẬT TOÁN ÁP DỤNG BỘ LỌC (DANH MỤC, TỪ KHÓA, KHOẢNG GIÁ, SẮP XẾP)
function applyAllFiltersAndRender() {
  var urlParams = new URLSearchParams(window.location.search);
  var searchQuery = urlParams.get("search") || "";
  var categoryParam = urlParams.get("category");

  if (categoryParam && currentCategoryFilter === "all") {
    currentCategoryFilter = categoryParam;
  }

  // A. Lọc theo Danh mục (Vòng lặp For)
  var step1 = [];
  for (var i = 0; i < rawProducts.length; i++) {
    var p = rawProducts[i];
    if (currentCategoryFilter === "all") {
      step1.push(p);
    } else {
      var normCat = removeVietnameseTones(p.category || "");
      var normName = removeVietnameseTones(p.name || "");
      var filterKey = removeVietnameseTones(currentCategoryFilter).replace(
        /-/g,
        " ",
      );

      if (
        normCat === currentCategoryFilter ||
        normCat.indexOf(currentCategoryFilter) !== -1 ||
        normName.indexOf(filterKey) !== -1
      ) {
        step1.push(p);
      }
    }
  }

  // B. Lọc theo Từ khóa tìm kiếm
  var step2 = [];
  if (searchQuery.trim() !== "") {
    var queryNorm = removeVietnameseTones(searchQuery);
    for (var j = 0; j < step1.length; j++) {
      var prod = step1[j];
      var nameNorm = removeVietnameseTones(prod.name);
      var descNorm = removeVietnameseTones(prod.description || "");
      if (
        nameNorm.indexOf(queryNorm) !== -1 ||
        descNorm.indexOf(queryNorm) !== -1
      ) {
        step2.push(prod);
      }
    }
  } else {
    step2 = step1;
  }

  // C. Lọc theo Khoảng giá (Vòng lặp For)
  var step3 = [];
  for (var k = 0; k < step2.length; k++) {
    var item = step2[k];
    if (
      item.price >= currentPriceRange.min &&
      item.price <= currentPriceRange.max
    ) {
      step3.push(item);
    }
  }

  // D. Sắp xếp danh sách (Sort)
  if (currentSortMode === "price-asc") {
    step3.sort(function (a, b) {
      return a.price - b.price;
    });
  } else if (currentSortMode === "price-desc") {
    step3.sort(function (a, b) {
      return b.price - a.price;
    });
  } else {
    // Mới nhất: ưu tiên isNew, sau đó đảo ngược thứ tự id
    step3.sort(function (a, b) {
      if (a.isNew && !b.isNew) return -1;
      if (!a.isNew && b.isNew) return 1;
      return b.id - a.id;
    });
  }

  currentFilteredProducts = step3;
  renderProductsGrid(currentFilteredProducts);
  highlightActiveSidebarMenu(currentCategoryFilter);
}

// 3. SINH MÃ HTML DỰA TRÊN DỮ LIỆU ĐÃ LỌC (DOM MANIPULATION)
function renderProductsGrid(products) {
  var grid = document.getElementById("products-grid");
  var noProducts = document.getElementById("no-products");
  var countBadge = document.getElementById("products-count");

  if (countBadge) {
    countBadge.textContent = products.length;
  }

  if (products.length === 0) {
    if (grid) grid.innerHTML = "";
    if (noProducts) {
      noProducts.style.display = "block";
      noProducts.innerHTML =
        '<div class="py-5 text-center">' +
        '  <i class="bi bi-search display-4 text-muted mb-3 d-block"></i>' +
        '  <h5 class="fw-bold text-dark">Không tìm thấy món nước nào phù hợp</h5>' +
        '  <p class="text-secondary small">Vui lòng thử điều chỉnh lại mức giá hoặc từ khóa tìm kiếm.</p>' +
        "</div>";
    }
    return;
  }

  if (noProducts) noProducts.style.display = "none";

  var html = "";
  for (var i = 0; i < products.length; i++) {
    var p = products[i];
    var formattedPrice =
      typeof formatPrice === "function"
        ? formatPrice(p.price)
        : p.price.toLocaleString("vi-VN") + "₫";

    html += '<div class="col">';
    html +=
      '  <div class="card h-100 border-0 shadow-sm product-card position-relative">';
    if (p.isBestSeller) {
      html +=
        '    <span class="badge bg-danger position-absolute top-0 start-0 m-3 py-1 px-2 rounded-pill fw-semibold shadow-sm" style="z-index: 2; font-size: 0.75rem;">Bán chạy</span>';
    }
    html +=
      '    <a href="product-detail.html?id=' +
      p.id +
      '" class="d-block overflow-hidden">';
    html +=
      '      <img src="' +
      p.image +
      '" class="card-img-top w-100 object-fit-cover" alt="' +
      p.name +
      '" style="height: 220px;" onerror="this.src=\'https://images.unsplash.com/photo-1558857563-b3710344c8b0?q=80&w=400\'">';
    html += "    </a>";
    html += '    <div class="card-body d-flex flex-column p-3">';
    html += '      <h5 class="card-title fw-bold fs-6 mb-1 text-truncate">';
    html +=
      '        <a href="product-detail.html?id=' +
      p.id +
      '" class="text-dark text-decoration-none">' +
      p.name +
      "</a>";
    html += "      </h5>";
    html +=
      '      <p class="card-text text-muted small mb-3 text-truncate" title="' +
      (p.description || "") +
      '">' +
      (p.description || "Thức uống thơm ngon hảo hạng từ Tiệm Trà TIỆM TRÀ SỮA.") +
      "</p>";
    html +=
      '      <div class="mt-auto d-flex justify-content-between align-items-center pt-2 border-top border-light-subtle">';
    html +=
      '        <span class="text-primary fw-bolder fs-5">' +
      formattedPrice +
      "</span>";
    html +=
      '        <a href="product-detail.html?id=' +
      p.id +
      '" class="btn btn-sm btn-primary rounded-pill px-3 fw-bold">';
    html += '          <i class="bi bi-plus-circle me-1"></i>Chọn món';
    html += "        </a>";
    html += "      </div>";
    html += "    </div>";
    html += "  </div>";
    html += "</div>";
  }

  if (grid) grid.innerHTML = html;
}

// 4. GẮN ACTIVE CHO SIDEBAR MENU
function highlightActiveSidebarMenu(category) {
  var filterButtons = document.querySelectorAll(".filter-btn");
  for (var i = 0; i < filterButtons.length; i++) {
    filterButtons[i].classList.remove("active");
    if (filterButtons[i].getAttribute("data-filter") === category) {
      filterButtons[i].classList.add("active");
    }
  }
}

function handleProductLoadError() {
  var grid = document.getElementById("products-grid");
  var noProducts = document.getElementById("no-products");
  if (grid) grid.innerHTML = "";
  if (noProducts) {
    noProducts.style.display = "block";
    noProducts.textContent = "Đã xảy ra sự cố khi tải danh sách thức uống.";
  }
}

// 5. GẮN CÁC SỰ KIỆN NÚT LỆNH & BỘ LỌC
document.addEventListener("DOMContentLoaded", function () {
  loadProductsFromDatabase();

  // Sự kiện Nút lọc danh mục
  var filterButtons = document.querySelectorAll(".filter-btn");
  for (var i = 0; i < filterButtons.length; i++) {
    filterButtons[i].addEventListener("click", function (e) {
      e.preventDefault();
      var selectedCategory = this.getAttribute("data-filter") || "all";
      currentCategoryFilter = selectedCategory;

      // Xóa query param search trên URL khi chọn danh mục
      var newUrl = window.location.pathname;
      if (selectedCategory !== "all") {
        newUrl += "?category=" + selectedCategory;
      }
      window.history.pushState({}, "", newUrl);

      applyAllFiltersAndRender();
    });
  }

  // Sự kiện Nút lọc khoảng giá
  var priceRadios = document.querySelectorAll('input[name="priceFilter"]');
  for (var p = 0; p < priceRadios.length; p++) {
    priceRadios[p].addEventListener("change", function () {
      if (this.id === "priceAll") {
        currentPriceRange = { min: 0, max: 99999999 };
      } else if (this.id === "price1") {
        currentPriceRange = { min: 0, max: 40000 };
      } else if (this.id === "price2") {
        currentPriceRange = { min: 40000, max: 60000 };
      }
      applyAllFiltersAndRender();
    });
  }

  // Sự kiện Chọn Sắp Xếp
  var sortSelect = document.getElementById("sortSelect");
  if (sortSelect) {
    sortSelect.addEventListener("change", function () {
      currentSortMode = this.value;
      applyAllFiltersAndRender();
    });
  }
});
