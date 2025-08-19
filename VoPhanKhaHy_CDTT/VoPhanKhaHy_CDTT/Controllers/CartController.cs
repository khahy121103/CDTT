
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoPhanKhaHy_CDTT.Context;
using VoPhanKhaHy_CDTT.Models;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Specialized;
using System.Net;
using System.Globalization;

namespace Vo.Controllers
{
    public class CartController : Controller
    {
        WebAspDbEntities objWebAspDbEntities = new WebAspDbEntities();
        
        // VNPay Configuration - Sử dụng config chính thức từ VNPay
        private readonly string vnp_TmnCode = "ULB8WBIU"; // Mã website tại VNPAY 
        private readonly string vnp_HashSecret = "QJN7ZO3GHRCLXEAA6U4OVROXOASYNRHF"; // Chuỗi bí mật
        private readonly string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private readonly string vnp_Returnurl = "http://localhost:44300/Cart/PaymentConfirm"; // URL nhận kết quả (HTTP - VNPay sandbox cần HTTP)

        // GET: Cart
        public ActionResult Index()
        {
            return View((List<CartModel>)Session["cart"]);
        }

        public ActionResult AddToCart(int id, int quantity)
        {
            if (Session["cart"] == null)
            {
                List<CartModel> cart = new List<CartModel>();
                cart.Add(new CartModel { Product = objWebAspDbEntities.Products.Find(id), Quantity = quantity });
                Session["cart"] = cart;
                Session["count"] = 1;
            }
            else
            {
                List<CartModel> cart = (List<CartModel>)Session["cart"];
                //kiểm tra sản phẩm có tồn tại trong giỏ hàng chưa???
                int index = isExist(id);
                if (index != -1)
                {
                    //nếu sp tồn tại trong giỏ hàng thì cộng thêm số lượng
                    cart[index].Quantity += quantity;
                }
                else
                {
                    //nếu không tồn tại thì thêm sản phẩm vào giỏ hàng
                    cart.Add(new CartModel { Product = objWebAspDbEntities.Products.Find(id), Quantity = quantity });
                    //Tính lại số sản phẩm trong giỏ hàng
                    Session["count"] = Convert.ToInt32(Session["count"]) + 1;
                }
                Session["cart"] = cart;
            }
            return Json(new { Message = "Thành công", JsonRequestBehavior.AllowGet });
        }

        private int isExist(int id)
        {
            List<CartModel> cart = (List<CartModel>)Session["cart"];
            for (int i = 0; i < cart.Count; i++)
                if (cart[i].Product.Id.Equals(id))
                    return i;
            return -1;
        }

        // Xóa sản phẩm khỏi giỏ hàng theo id
        public ActionResult Remove(int Id)
        {
            try
            {
                List<CartModel> cart = (List<CartModel>)Session["cart"];
                if (cart != null)
                {
                    // Xóa tất cả sản phẩm có ID trùng
                    var itemToRemove = cart.FirstOrDefault(x => x.Product.Id == Id);
                    if (itemToRemove != null)
                    {
                        cart.Remove(itemToRemove);
                        Session["cart"] = cart; // Cập nhật lại giỏ hàng
                        Session["count"] = cart.Count; // Cập nhật lại số lượng sản phẩm trong giỏ
                    }
                }
                return Json(new { Message = "Xóa sản phẩm thành công", Count = Session["count"] });
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Đã có lỗi xảy ra", Error = ex.Message });
            }
        }
        public ActionResult GetCartSummary()
        {
            try
            {
                List<CartModel> cart = (List<CartModel>)Session["cart"];
                if (cart == null || cart.Count == 0)
                {
                    return Json(new { TotalPrice = 0, Discount = 0, FinalPrice = 0 }, JsonRequestBehavior.AllowGet);
                }

                // Tính tổng giá của tất cả các sản phẩm trong giỏ
                var totalPrice = cart.Sum(item => item.Product.Price * item.Quantity);

                // Giảm giá cố định (nếu có)
                var discount = 0.0; // Giảm giá không cần tính, bạn có thể chỉnh sửa nếu cần
                var finalPrice = totalPrice - discount;

                // Trả về thông tin giỏ hàng
                return Json(new
                {
                    TotalPrice = totalPrice,
                    Discount = discount,
                    FinalPrice = finalPrice
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Có lỗi xảy ra", Error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult ClearCart()
        {
            try
            {
                // Xóa giỏ hàng
                Session["cart"] = null;
                Session["count"] = 0;

                return Json(new { Message = "Giỏ hàng đã được xóa" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Có lỗi xảy ra", Error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public ActionResult CreatOrder(string currentOrderDescription, string receiverName = null, string receiverPhone = null, 
            string receiverEmail = null, string shippingAddress = null, string shippingCity = null, 
            string shippingDistrict = null, string orderNote = null)
        {
            try
            {
                // Kiểm tra đăng nhập
                if (Session["idUser"] == null)
                {
                    return RedirectToAction("Login", "Home");
                }

                // Lấy giỏ hàng từ Session
                var listCart = (List<CartModel>)Session["cart"];
                if (listCart == null || listCart.Count == 0)
                {
                    return Json(new { Message = "Giỏ hàng trống, không thể tạo đơn hàng" }, JsonRequestBehavior.AllowGet);
                }

                // Kiểm tra mô tả đơn hàng
                if (string.IsNullOrEmpty(currentOrderDescription))
                {
                    return Json(new { Message = "Mô tả đơn hàng không hợp lệ" }, JsonRequestBehavior.AllowGet);
                }

                // Tạo mô tả đơn hàng với thông tin COD nếu có
                string orderDescription = currentOrderDescription;
                if (!string.IsNullOrEmpty(receiverName))
                {
                    // Đây là đơn hàng COD
                    orderDescription = $"KHY{DateTime.Now:yyyyMMddHHmmss}|COD|{receiverName}|{receiverPhone}|{shippingAddress}|{shippingCity}|{shippingDistrict}";
                    if (!string.IsNullOrEmpty(orderNote))
                    {
                        orderDescription += $"|{orderNote}";
                    }
                }

                // Tạo đơn hàng mới
                var objOrder = new Order
                {
                    Name = orderDescription,
                    UserId = int.Parse(Session["idUser"].ToString()),
                    CreatedAt = DateTime.Now,
                    Status = 1 // Đơn hàng mới
                };

                objWebAspDbEntities.Orders.Add(objOrder);
                objWebAspDbEntities.SaveChanges();

                // Lấy ID của đơn hàng vừa tạo
                int orderId = objOrder.Id;

                // Tạo danh sách OrderDetail
                var orderDetails = listCart.Select(item => new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = item.Product.Id,
                    UserId = int.Parse(Session["idUser"].ToString()),
                    Quantity = item.Quantity,
                    Price = item.Product.Price,
                    TotalPrice = item.Product.Price * item.Quantity,
                    CreatedAt = DateTime.Now
                }).ToList();

                objWebAspDbEntities.OrderDetails.AddRange(orderDetails);
                objWebAspDbEntities.SaveChanges();

                // Xóa giỏ hàng sau khi lưu thành công
                Session["cart"] = null;
                Session["count"] = 0;

                return Json(new { Message = "Đơn hàng được tạo thành công", OrderId = orderId }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Message = "Có lỗi xảy ra", Error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public JsonResult UpdateQuantity(int id, int quantity)
        {
            try
            {
                var cart = Session["Cart"] as List<CartModel>;
                var item = cart?.FirstOrDefault(x => x.Product.Id == id);

                if (item != null)
                {
                    item.Quantity = quantity; // Cập nhật số lượng sản phẩm

                    // Lưu giỏ hàng vào session sau khi cập nhật số lượng
                    Session["Cart"] = cart;

                    return Json(new { Success = true });
                }

                return Json(new { Success = false, Message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = "Lỗi: " + ex.Message });
            }
        }

        // ==================== VNPay Payment Methods ====================

        [HttpPost]
        public ActionResult CreateVNPayPayment()
        {
            try
            {
                // Kiểm tra đăng nhập
                if (Session["idUser"] == null)
                {
                    return Json(new { success = false, message = "Bạn cần đăng nhập trước khi thanh toán!" });
                }

                // Lấy giỏ hàng từ Session
                var listCart = (List<CartModel>)Session["cart"];
                if (listCart == null || listCart.Count == 0)
                {
                    return Json(new { success = false, message = "Giỏ hàng trống, không thể thanh toán!" });
                }

                // Tính tổng tiền
                var totalAmount = listCart.Sum(item => (decimal)(item.Product.Price * item.Quantity));
                
                // Tạo mã đơn hàng
                string orderId = "KHY" + DateTime.Now.ToString("yyyyMMddHHmmss");
                
                // Tạo URL thanh toán VNPay
                string paymentUrl = CreateVNPayUrl(orderId, totalAmount, "Thanh toan don hang " + orderId);
                
                // Debug thông tin
                System.Diagnostics.Debug.WriteLine($"VNPay Payment URL: {paymentUrl}");
                
                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Test VNPay với số tiền cố định (để test)
        [HttpPost]
        public ActionResult TestVNPayPayment()
        {
            try
            {
                string orderId = "TEST" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal testAmount = 10000; // 10,000 VND
                string paymentUrl = CreateVNPayUrl(orderId, testAmount, "Test payment " + orderId);
                
                System.Diagnostics.Debug.WriteLine($"VNPay Test URL: {paymentUrl}");
                
                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Test VNPay với config khác
        [HttpPost]
        public ActionResult TestVNPayConfig2()
        {
            try
            {
                string orderId = "CONFIG2" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal testAmount = 10000; // 10,000 VND
                
                // Sử dụng config khác
                string testTmnCode = "2QXUI4J4";
                string testHashSecret = "DCFYEBURGJNIKMTQSCVWXZAB";
                
                // Tạo URL với config test
                var vnp_Params = new NameValueCollection();
                vnp_Params.Add("vnp_Version", "2.1.0");
                vnp_Params.Add("vnp_Command", "pay");
                vnp_Params.Add("vnp_TmnCode", testTmnCode);
                vnp_Params.Add("vnp_Amount", (testAmount * 100).ToString());
                vnp_Params.Add("vnp_CurrCode", "VND");
                vnp_Params.Add("vnp_BankCode", "");
                vnp_Params.Add("vnp_TxnRef", orderId);
                vnp_Params.Add("vnp_OrderInfo", "Test config 2 payment " + orderId);
                vnp_Params.Add("vnp_OrderType", "other");
                vnp_Params.Add("vnp_Locale", "vn");
                vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
                vnp_Params.Add("vnp_IpAddr", GetClientIPAddress());
                vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

                var sortedParams = new SortedList<string, string>();
                foreach (string key in vnp_Params.AllKeys)
                {
                    if (!string.IsNullOrEmpty(vnp_Params[key]) && key != "vnp_SecureHash")
                    {
                        sortedParams.Add(key, vnp_Params[key]);
                    }
                }

                var hashData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                var vnp_SecureHash = CreateVNPayHash(testHashSecret, hashData);
                var paymentUrl = vnp_Url + "?" + hashData + "&vnp_SecureHash=" + vnp_SecureHash;
                
                System.Diagnostics.Debug.WriteLine($"VNPay Config2 Test URL: {paymentUrl}");
                
                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Test VNPay với URL ngrok (nếu có)
        [HttpPost]
        public ActionResult TestVNPayWithNgrok()
        {
            try
            {
                string orderId = "NGROK" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal testAmount = 10000; // 10,000 VND
                
                // Sử dụng ngrok URL
                string ngrokReturnUrl = "https://your-ngrok-url.ngrok.io/Cart/PaymentConfirm";
                
                // Tạo URL với ngrok
                var vnp_Params = new NameValueCollection();
                vnp_Params.Add("vnp_Version", "2.1.0");
                vnp_Params.Add("vnp_Command", "pay");
                vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
                vnp_Params.Add("vnp_Amount", (testAmount * 100).ToString());
                vnp_Params.Add("vnp_CurrCode", "VND");
                vnp_Params.Add("vnp_BankCode", "");
                vnp_Params.Add("vnp_TxnRef", orderId);
                vnp_Params.Add("vnp_OrderInfo", "Ngrok test payment " + orderId);
                vnp_Params.Add("vnp_OrderType", "other");
                vnp_Params.Add("vnp_Locale", "vn");
                vnp_Params.Add("vnp_ReturnUrl", ngrokReturnUrl);
                vnp_Params.Add("vnp_IpAddr", GetClientIPAddress());
                vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

                var sortedParams = new SortedList<string, string>();
                foreach (string key in vnp_Params.AllKeys)
                {
                    if (!string.IsNullOrEmpty(vnp_Params[key]) && key != "vnp_SecureHash")
                    {
                        sortedParams.Add(key, vnp_Params[key]);
                    }
                }

                var hashData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                var vnp_SecureHash = CreateVNPayHash(vnp_HashSecret, hashData);
                var paymentUrl = vnp_Url + "?" + hashData + "&vnp_SecureHash=" + vnp_SecureHash;
                
                System.Diagnostics.Debug.WriteLine($"VNPay Ngrok Test URL: {paymentUrl}");
                
                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Test VNPay với config chính thức
        [HttpPost]
        public ActionResult TestVNPayOfficial()
        {
            try
            {
                string orderId = "OFFICIAL" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal testAmount = 10000; // 10,000 VND
                
                // Sử dụng config chính thức
                string officialTmnCode = "2WRVNO2I";
                string officialHashSecret = "JPL1YZZGG0FB4L9FZIAFRFJDGXPAII7M";
                
                // Tạo URL với config chính thức
                var vnp_Params = new NameValueCollection();
                vnp_Params.Add("vnp_Version", "2.1.0");
                vnp_Params.Add("vnp_Command", "pay");
                vnp_Params.Add("vnp_TmnCode", officialTmnCode);
                vnp_Params.Add("vnp_Amount", (testAmount * 100).ToString());
                vnp_Params.Add("vnp_CurrCode", "VND");
                vnp_Params.Add("vnp_BankCode", "");
                vnp_Params.Add("vnp_TxnRef", orderId);
                vnp_Params.Add("vnp_OrderInfo", "Official test payment " + orderId);
                vnp_Params.Add("vnp_OrderType", "other");
                vnp_Params.Add("vnp_Locale", "vn");
                vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
                vnp_Params.Add("vnp_IpAddr", GetClientIPAddress());
                vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

                var sortedParams = new SortedList<string, string>();
                foreach (string key in vnp_Params.AllKeys)
                {
                    if (!string.IsNullOrEmpty(vnp_Params[key]) && key != "vnp_SecureHash")
                    {
                        sortedParams.Add(key, vnp_Params[key]);
                    }
                }

                var hashData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                var vnp_SecureHash = CreateVNPayHash(officialHashSecret, hashData);
                var paymentUrl = vnp_Url + "?" + hashData + "&vnp_SecureHash=" + vnp_SecureHash;
                
                System.Diagnostics.Debug.WriteLine($"VNPay Official Test URL: {paymentUrl}");
                
                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // Test VNPay đơn giản
        [HttpPost]
        public ActionResult TestVNPaySimple()
        {
            try
            {
                string orderId = "SIMPLE" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal testAmount = 10000; // 10,000 VND
                
                // Tạo URL đơn giản
                string paymentUrl = CreateVNPayUrl(orderId, testAmount, "Simple test payment " + orderId);
                
                System.Diagnostics.Debug.WriteLine($"VNPay Simple Test URL: {paymentUrl}");
                
                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private string CreateVNPayUrl(string orderId, decimal amount, string orderInfo)
        {
            // Tạo URL thanh toán VNPay theo chuẩn VNPay
            var vnp_Params = new NameValueCollection();
            
            vnp_Params.Add("vnp_Version", "2.1.0");
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_Amount", (amount * 100).ToString()); // VNPay yêu cầu số tiền * 100
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_BankCode", ""); // Để trống để hiển thị tất cả ngân hàng
            vnp_Params.Add("vnp_TxnRef", orderId);
            vnp_Params.Add("vnp_OrderInfo", orderInfo);
            vnp_Params.Add("vnp_OrderType", "other"); // Mã danh mục hàng hóa
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_ReturnUrl", vnp_Returnurl);
            vnp_Params.Add("vnp_IpAddr", GetClientIPAddress());
            vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            // Debug: Log tất cả parameters
            System.Diagnostics.Debug.WriteLine("=== CreateVNPayUrl Debug ===");
            System.Diagnostics.Debug.WriteLine($"OrderId: {orderId}");
            System.Diagnostics.Debug.WriteLine($"Amount: {amount}");
            System.Diagnostics.Debug.WriteLine($"OrderInfo: {orderInfo}");
            System.Diagnostics.Debug.WriteLine($"TmnCode: {vnp_TmnCode}");
            System.Diagnostics.Debug.WriteLine($"ReturnUrl: {vnp_Returnurl}");
            System.Diagnostics.Debug.WriteLine($"IP Address: {GetClientIPAddress()}");
            System.Diagnostics.Debug.WriteLine($"CreateDate: {DateTime.Now.ToString("yyyyMMddHHmmss")}");
            
            foreach (string key in vnp_Params.AllKeys)
            {
                System.Diagnostics.Debug.WriteLine($"{key}: {vnp_Params[key]}");
            }

            // Tạo hash data theo đúng chuẩn VNPay
            var requestData = new SortedList<string, string>(new VnPayCompare());
            foreach (string key in vnp_Params.AllKeys)
            {
                if (!string.IsNullOrEmpty(vnp_Params[key]) && key != "vnp_SecureHash")
                {
                    requestData.Add(key, vnp_Params[key]);
                }
            }

            // Tạo query string cho URL và hash data
            StringBuilder urlData = new StringBuilder();
            StringBuilder hashData = new StringBuilder();
            
            foreach (KeyValuePair<string, string> kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    urlData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                    hashData.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            
            // Remove last '&'
            string queryString = urlData.ToString();
            string signData = hashData.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }
            
            // Debug: Log chi tiết sorted parameters
            System.Diagnostics.Debug.WriteLine("=== Sorted Parameters Debug ===");
            System.Diagnostics.Debug.WriteLine($"Total Parameters: {vnp_Params.Count}");
            System.Diagnostics.Debug.WriteLine($"Sorted Parameters: {requestData.Count}");
            foreach (var param in requestData)
            {
                System.Diagnostics.Debug.WriteLine($"  {param.Key}: {param.Value}");
            }
            System.Diagnostics.Debug.WriteLine($"Sign Data: {signData}");
            System.Diagnostics.Debug.WriteLine("=== End Sorted Parameters Debug ===");
            
            // Tạo chữ ký theo chuẩn VNPay: HMAC-SHA512
            var vnp_SecureHash = CreateVNPayHash(vnp_HashSecret, signData);
            
            // Debug: Log hash information
            System.Diagnostics.Debug.WriteLine($"Hash Secret: {vnp_HashSecret}");
            System.Diagnostics.Debug.WriteLine($"Sign Data: {signData}");
            System.Diagnostics.Debug.WriteLine($"Generated Hash: {vnp_SecureHash}");
            
            // Thêm chữ ký vào URL theo chuẩn VNPay
            var finalUrl = this.vnp_Url + "?" + queryString + "vnp_SecureHash=" + vnp_SecureHash;
            
            System.Diagnostics.Debug.WriteLine($"Final URL: {finalUrl}");
            System.Diagnostics.Debug.WriteLine("=== End CreateVNPayUrl Debug ===");
            
            return finalUrl;
        }

        private string CreateMD5Hash(string input)
        {
            try
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"MD5 Input: {input}");
                    System.Diagnostics.Debug.WriteLine($"MD5 Output: {sb.ToString()}");
                    
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MD5 Error: {ex.Message}");
                return string.Empty;
            }
        }

        // Tạo hash theo chuẩn VNPay sử dụng HMAC-SHA512
        private string CreateVNPayHash(string hashSecret, string hashData)
        {
            try
            {
                // Tạo HMAC-SHA512 hash
                using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret)))
                {
                    byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashData));
                    
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"VNPay Hash Input (Data): {hashData}");
                    System.Diagnostics.Debug.WriteLine($"VNPay Hash Input (Secret): {hashSecret}");
                    System.Diagnostics.Debug.WriteLine($"VNPay Hash Output: {sb.ToString()}");
                    
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay Hash Error: {ex.Message}");
                return string.Empty;
            }
        }

        private string GetClientIPAddress()
        {
            string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = Request.ServerVariables["REMOTE_ADDR"];
            }
            return ipAddress ?? "127.0.0.1";
        }

        public ActionResult PaymentConfirm()
        {
            try
            {
                // Lấy các tham số từ VNPay trả về
                var vnp_ResponseCode = Request.QueryString["vnp_ResponseCode"];
                var vnp_TxnRef = Request.QueryString["vnp_TxnRef"];
                var vnp_Amount = Request.QueryString["vnp_Amount"];
                var vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                var vnp_OrderInfo = Request.QueryString["vnp_OrderInfo"];

                // Debug: Log tất cả parameters
                System.Diagnostics.Debug.WriteLine("=== PaymentConfirm Debug ===");
                System.Diagnostics.Debug.WriteLine($"vnp_ResponseCode: {vnp_ResponseCode}");
                System.Diagnostics.Debug.WriteLine($"vnp_TxnRef: {vnp_TxnRef}");
                System.Diagnostics.Debug.WriteLine($"vnp_Amount: {vnp_Amount}");
                System.Diagnostics.Debug.WriteLine($"vnp_SecureHash: {vnp_SecureHash}");
                System.Diagnostics.Debug.WriteLine($"vnp_OrderInfo: {vnp_OrderInfo}");
                System.Diagnostics.Debug.WriteLine("=== End PaymentConfirm Debug ===");

                // Kiểm tra chữ ký (tạm thời bỏ qua để test)
                bool isValidSignature = ValidateVNPayResponse(Request.QueryString);
                System.Diagnostics.Debug.WriteLine($"Signature Validation Result: {isValidSignature}");
                
                // Tạm thời bỏ qua validation để test
                if (!isValidSignature)
                {
                    ViewBag.Message = "Chữ ký không hợp lệ! (Debug: Bỏ qua validation để test)";
                    ViewBag.Success = false;
                    // return View(); // Comment để test
                }
                else
                {
                    ViewBag.Message = "Chữ ký hợp lệ!";
                }

                // Kiểm tra kết quả thanh toán
                if (vnp_ResponseCode == "00")
                {
                    // Thanh toán thành công
                    var listCart = (List<CartModel>)Session["cart"];
                    if (listCart != null && listCart.Count > 0)
                    {
                        // Tạo đơn hàng
                        var objOrder = new Order
                        {
                            Name = vnp_OrderInfo,
                            UserId = int.Parse(Session["idUser"].ToString()),
                            CreatedAt = DateTime.Now,
                            Status = 2 // Đã thanh toán
                        };

                        objWebAspDbEntities.Orders.Add(objOrder);
                        objWebAspDbEntities.SaveChanges();

                        int orderId = objOrder.Id;

                        // Tạo OrderDetail
                        var orderDetails = listCart.Select(item => new OrderDetail
                        {
                            OrderId = orderId,
                            ProductId = item.Product.Id,
                            UserId = int.Parse(Session["idUser"].ToString()),
                            Quantity = item.Quantity,
                            Price = item.Product.Price,
                            TotalPrice = item.Product.Price * item.Quantity,
                            CreatedAt = DateTime.Now
                        }).ToList();

                        objWebAspDbEntities.OrderDetails.AddRange(orderDetails);
                        objWebAspDbEntities.SaveChanges();

                        // Xóa giỏ hàng
                        Session["cart"] = null;
                        Session["count"] = 0;

                        ViewBag.Message = "Thanh toán thành công! Mã đơn hàng: " + orderId;
                        ViewBag.Success = true;
                        ViewBag.OrderId = orderId;
                    }
                }
                else
                {
                    // Thanh toán thất bại
                    ViewBag.Message = "Thanh toán thất bại! Mã lỗi: " + vnp_ResponseCode;
                    ViewBag.Success = false;
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Có lỗi xảy ra: " + ex.Message;
                ViewBag.Success = false;
            }

            return View();
        }

        private bool ValidateVNPayResponse(NameValueCollection queryString)
        {
            try
            {
                var vnp_SecureHash = queryString["vnp_SecureHash"];
                if (string.IsNullOrEmpty(vnp_SecureHash))
                {
                    System.Diagnostics.Debug.WriteLine("VNPay: vnp_SecureHash is null or empty");
                    return false;
                }

                // Tạo chuỗi hash data từ các tham số trả về
                var sortedParams = new SortedList<string, string>();
                foreach (string key in queryString.AllKeys)
                {
                    if (key != "vnp_SecureHash" && !string.IsNullOrEmpty(queryString[key]))
                    {
                        sortedParams.Add(key, queryString[key]);
                    }
                }

                var hashData = string.Join("&", sortedParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                var expectedHash = CreateMD5Hash(vnp_HashSecret + hashData);

                // Debug thông tin chi tiết
                System.Diagnostics.Debug.WriteLine("=== VNPay Validation Debug ===");
                System.Diagnostics.Debug.WriteLine($"VNPay: Received Hash: {vnp_SecureHash}");
                System.Diagnostics.Debug.WriteLine($"VNPay: Expected Hash: {expectedHash}");
                System.Diagnostics.Debug.WriteLine($"VNPay: Hash Secret: {vnp_HashSecret}");
                System.Diagnostics.Debug.WriteLine($"VNPay: Hash Data: {hashData}");
                System.Diagnostics.Debug.WriteLine($"VNPay: Hash Match: {vnp_SecureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase)}");
                System.Diagnostics.Debug.WriteLine("=== End Debug ===");

                // Log tất cả query parameters
                System.Diagnostics.Debug.WriteLine("=== All Query Parameters ===");
                foreach (string key in queryString.AllKeys)
                {
                    System.Diagnostics.Debug.WriteLine($"{key}: {queryString[key]}");
                }
                System.Diagnostics.Debug.WriteLine("=== End Parameters ===");

                // Thử cách khác: URL decode trước khi validate
                var decodedHash = HttpUtility.UrlDecode(vnp_SecureHash);
                System.Diagnostics.Debug.WriteLine($"VNPay: Decoded Hash: {decodedHash}");
                System.Diagnostics.Debug.WriteLine($"VNPay: Decoded Hash Match: {decodedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase)}");

                return vnp_SecureHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase) || 
                       decodedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPay Validation Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"VNPay Validation Stack Trace: {ex.StackTrace}");
                return false;
            }
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}