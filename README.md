# 🧠 FriendWithBooks - Backend API

Đây là phần Backend của đồ án FriendwithBooks, một ứng dụng web để bán sách qua mạng Internet.

## 🛠️ Công nghệ sử dụng

- ASP.NET Core Web API
- SignalR
- Google Firebase

## ⚙️ Cấu hình ban đầu

### 1. Sửa chuỗi kết nối cơ sở dữ liệu

Mở file `appsettings.json`, tìm và thay:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;"
}
```

➡️ bằng chuỗi kết nối tới cơ sở dữ liệu của bạn.

---

### 2. Gắn file Firebase key

Đặt file `firebase_secret.json` (chứa key Firebase Admin SDK) vào thư mục gốc của project hoặc nơi bạn cấu hình trong mã.

> ⚠️ **Không commit file này lên GitHub!**  
> Thêm vào `.gitignore` để tránh lộ credentials.

---

## ▶️ Chạy API

```bash
dotnet restore
dotnet build
dotnet run
```

API sẽ khởi chạy tại `https://localhost:7129` (hoặc port bạn cấu hình).

---

## 🎉 Have fun!
