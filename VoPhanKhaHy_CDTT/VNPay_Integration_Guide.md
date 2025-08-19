# Hướng dẫn tích hợp VNPay

## 📋 Tổng quan

Hệ thống thanh toán VNPay đã được tích hợp hoàn chỉnh vào website với các tính năng:

- ✅ Thanh toán VNPay thật (với giỏ hàng)
- ✅ Test VNPay (10,000 VND cố định)
- ✅ Xác thực chữ ký MD5
- ✅ Xử lý callback từ VNPay
- ✅ Tạo đơn hàng tự động
- ✅ Giao diện đẹp và responsive

## 🔧 Cấu hình hiện tại

### Sandbox Configuration
```csharp
private readonly string vnp_TmnCode = "2WRVNO2I"; // Mã website tại VNPAY 
private readonly string vnp_HashSecret = "JPL1YZZGG0FB4L9FZIAFRFJDGXPAII7M"; // Chuỗi bí mật
private readonly string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
private readonly string vnp_Returnurl = "http://localhost:44300/Cart/PaymentConfirm";
```

### Production Configuration
Khi deploy production, cần thay đổi:
```csharp
private readonly string vnp_Url = "https://pay.vnpay.vn/vpcpay.html";
private readonly string vnp_Returnurl = "https://yourdomain.com/Cart/PaymentConfirm";
```

## 🚀 Cách sử dụng

### 1. Thanh toán thật
1. Thêm sản phẩm vào giỏ hàng
2. Nhấn nút **"Thanh toán VNPay"** (màu vàng)
3. Chuyển đến VNPay sandbox
4. Chọn ngân hàng và thanh toán
5. Quay về trang kết quả

### 2. Test thanh toán
1. Nhấn nút **"Test VNPay (10K)"** (màu xanh)
2. Chuyển đến VNPay với số tiền 10,000 VND
3. Test mà không cần sản phẩm trong giỏ

## 🔐 Bảo mật

### Chữ ký MD5
- Tạo chữ ký: `MD5(vnp_HashSecret + hashData)`
- Validate chữ ký khi nhận callback
- Debug logging để theo dõi

### Validation
```csharp
private bool ValidateVNPayResponse(NameValueCollection queryString)
{
    // Loại bỏ vnp_SecureHash
    // Sắp xếp theo alphabet
    // Tạo hash và so sánh
}
```

## 📊 Luồng thanh toán

```
1. User nhấn "Thanh toán VNPay"
2. CreateVNPayPayment() tạo URL
3. Redirect đến VNPay
4. User thanh toán trên VNPay
5. VNPay callback về PaymentConfirm
6. Validate chữ ký
7. Tạo đơn hàng nếu thành công
8. Hiển thị kết quả
```

## 🎨 Giao diện

### Nút thanh toán
- **Thanh toán VNPay**: Gradient vàng-cam
- **Test VNPay**: Gradient xanh-tím
- **Loading animation**: Spinner khi xử lý

### Trang kết quả
- **Thành công**: Icon check xanh, animation bounce
- **Thất bại**: Icon X đỏ, animation shake
- **Responsive**: Tương thích mobile

## 🐛 Debug và Troubleshooting

### Debug Logging
```csharp
System.Diagnostics.Debug.WriteLine($"VNPay Payment URL: {paymentUrl}");
System.Diagnostics.Debug.WriteLine($"VNPay: Received Hash: {vnp_SecureHash}");
System.Diagnostics.Debug.WriteLine($"VNPay: Expected Hash: {expectedHash}");
```

### Lỗi thường gặp
1. **"Sai chữ ký"**: Kiểm tra vnp_HashSecret và URL return
2. **"Giỏ hàng trống"**: Thêm sản phẩm trước khi thanh toán
3. **"Chưa đăng nhập"**: Đăng nhập trước khi thanh toán

## 📱 Test Cards (Sandbox)

### Thẻ thành công
- Ngân hàng: NCB
- Số thẻ: 9704198526191432198
- Tên chủ thẻ: NGUYEN VAN A
- Ngày phát hành: 07/15
- OTP: 123456

### Thẻ thất bại
- Số thẻ: 9704198526191432199
- OTP: 123456

## 🔄 Cập nhật Production

### 1. Thay đổi URL
```csharp
private readonly string vnp_Url = "https://pay.vnpay.vn/vpcpay.html";
```

### 2. Cập nhật return URL
```csharp
private readonly string vnp_Returnurl = "https://yourdomain.com/Cart/PaymentConfirm";
```

### 3. Kiểm tra SSL
- Đảm bảo website có SSL certificate
- URL return phải là HTTPS

### 4. Test kỹ lưỡng
- Test thanh toán thành công
- Test thanh toán thất bại
- Test cancel thanh toán
- Test timeout

## 📞 Hỗ trợ

### VNPay Support
- Hotline: 1900.5555.77
- Email: hotrovnpay@vnpay.vn
- Website: https://vnpay.vn

### Technical Support
- Kiểm tra debug logs
- Verify configuration
- Test với sandbox trước
- Deploy production sau khi test OK

## ✅ Checklist Production

- [ ] Thay đổi URL từ sandbox sang production
- [ ] Cập nhật return URL thành HTTPS
- [ ] Test thanh toán thành công
- [ ] Test thanh toán thất bại
- [ ] Test cancel thanh toán
- [ ] Kiểm tra SSL certificate
- [ ] Verify chữ ký MD5
- [ ] Test trên mobile
- [ ] Backup database
- [ ] Monitor logs

---

**Lưu ý**: Luôn test kỹ lưỡng trên sandbox trước khi deploy production! 🚀
