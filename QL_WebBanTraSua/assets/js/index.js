// ==========================================================
// TIỆM TRÀ SỮA - TRANG CHỦ (index.js)
// Nạp các thức uống nổi bật & bán chạy nhất lên Trang Chủ
// ==========================================================

document.addEventListener("DOMContentLoaded", function () {
  loadFeaturedDrinks();
});

// 1. DÙNG FETCH API NẠP DỮ LIỆU TỪ CSDL JSON
function loadFeaturedDrinks() {
  fetch("../data/products.json")
    .then(function (response) {
      if (!response.ok) throw new Error("Không thể nạp dữ liệu món nước.");
      return response.json();
    })
    .then(function (products) {
      // 2. THUẬT TOÁN LỌC CÁC MÓN BEST-SELLER VÀ MÓN MỚI (VÒNG LẶP FOR)
      var featured = [];
      for (var i = 0; i < products.length; i++) {
        var p = products[i];
        // Lấy các món Bán chạy hoặc Mới
        if (p.isBestSeller || p.isNew) {
          featured.push(p);
        }
        if (featured.length >= 6) break; // Giới hạn 6 món tiêu biểu nhất
      }

      // Nếu ít hơn 6, lấy bù từ đầu danh sách
      if (featured.length < 6) {
        for (var j = 0; j < products.length; j++) {
          var exists = false;
          for (var k = 0; k < featured.length; k++) {
            if (featured[k].id === products[j].id) {
              exists = true;
              break;
            }
          }
          if (!exists) {
            featured.push(products[j]);
          }
          if (featured.length >= 6) break;
        }
      }

      renderFeaturedGrid(featured);
    })
    .catch(function (error) {
      console.log("Dùng giao diện mẫu tĩnh trên trang chủ:", error);
    });
}

// 3. SINH HTML RENDER LÊN LƯỚI SẢN PHẨM TRANG CHỦ (DOM)
function renderFeaturedGrid(items) {
  var grid = document.getElementById("latest-products-grid");
  if (!grid) return;

  var html = "";
  for (var i = 0; i < items.length; i++) {
    var p = items[i];
    var formattedPrice =
      typeof formatPrice === "function"
        ? formatPrice(p.price)
        : p.price.toLocaleString("vi-VN") + "₫";

    html += '<div class="col-md-4 col-sm-6">';
    html +=
      '  <div class="card h-100 border-0 shadow-sm product-card position-relative">';
    if (p.isBestSeller) {
      html +=
        '    <span class="badge bg-danger position-absolute top-0 start-0 m-3 py-1 px-2 rounded-pill fw-semibold shadow-sm" style="z-index: 2; font-size: 0.75rem;">Bán chạy</span>';
    } else if (p.isNew) {
      html +=
        '    <span class="badge bg-success position-absolute top-0 start-0 m-3 py-1 px-2 rounded-pill fw-semibold shadow-sm" style="z-index: 2; font-size: 0.75rem;">Món mới</span>';
    }
    html +=
      '    <a href="customer/product-detail.html?id=' +
      p.id +
      '" class="d-block overflow-hidden">';
    html +=
      '      <img src="' +
      p.image +
      '" class="card-img-top w-100 object-fit-cover" alt="' +
      p.name +
      '" style="height: 240px;" onerror="this.src=\'https://images.unsplash.com/photo-1541658016709-82535e94bc69?q=80&w=400\'">';
    html += "    </a>";
    html += '    <div class="card-body d-flex flex-column p-3">';
    html += '      <h5 class="card-title fw-bold fs-6 mb-1 text-truncate">';
    html +=
      '        <a href="customer/product-detail.html?id=' +
      p.id +
      '" class="text-dark text-decoration-none">' +
      p.name +
      "</a>";
    html += "      </h5>";
    html +=
      '      <p class="card-text text-muted small mb-3 text-truncate" title="' +
      (p.description || "") +
      '">' +
      (p.description || "Thức uống thơm ngon từ Tiệm Trà TIỆM TRÀ SỮA.") +
      "</p>";
    html +=
      '      <div class="mt-auto d-flex justify-content-between align-items-center pt-2 border-top border-light-subtle">';
    html +=
      '        <span class="text-primary fw-bolder fs-5">' +
      formattedPrice +
      "</span>";
    html +=
      '        <a href="customer/product-detail.html?id=' +
      p.id +
      '" class="btn btn-primary rounded-pill px-3 fw-bold">';
    html += '          <i class="bi bi-plus-circle me-1"></i>Chọn món';
    html += "        </a>";
    html += "      </div>";
    html += "    </div>";
    html += "  </div>";
    html += "</div>";
  }

  grid.innerHTML = html;
}
