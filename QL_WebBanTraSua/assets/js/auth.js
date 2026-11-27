// ==========================================================
// CHỈ CHỨA DỮ LIỆU USER VÀ HÀM TẢI DỮ LIỆU CƠ BẢN
// ==========================================================
var users = [
  {
    user_id: 1001,
    full_name: "Nguyễn Văn A",
    email: "nguyenvana@example.com",
    password: "khach1001@",
    phone_number: "0987654321",
    address: "Số 6, Đường XYZ, Quận ABC",
    joined_date: "2025-12-11T10:00:00Z",
  },
  {
    user_id: 1002,
    full_name: "Trần Thị B",
    email: "tranthib@example.com",
    password: "khach1002@",
    phone_number: "0912345678",
    address: "Phòng 601, Chung cư N6",
    joined_date: "2025-12-11T10:05:00Z",
  },
];

function loadUsers() {
  var userPath = (window.location.pathname.indexOf("/customer/") !== -1 || window.location.pathname.indexOf("/auth/") !== -1) ? "../../data/user.json" : "../data/user.json";
  return fetch(userPath).catch(function() { return fetch("../data/user.json"); })
    .then(function (response) {
      if (response.ok) return response.json();
      return { users: [] };
    })
    .then(function (data) {
      if (data && data.users && data.users.length > 0) {
        users = data.users;
      }

      // Nạp thêm user đã đăng ký trên local storage
      var registeredUsers = localStorage.getItem("registeredUsers");
      if (registeredUsers) {
        var parsed = JSON.parse(registeredUsers);
        for (var i = 0; i < parsed.length; i++) {
          var isExist = false;
          for (var j = 0; j < users.length; j++) {
            if (users[j].user_id === parsed[i].user_id) {
              isExist = true;
              break;
            }
          }
          if (isExist === false) {
            users.push(parsed[i]);
          }
        }
      }
    })
    .catch(function (error) {
      console.log("Dùng data user mặc định");
    });
}
