using Google;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoPhanKhaHy_CDTT.Context;
using VoPhanKhaHy_CDTT.Models;


namespace VoPhanKhaHy_CDTT.Controllers
{
    public class CartController : Controller
    {
        WebAspDbEntities objWebAspDbEntities = new WebAspDbEntities();
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
        public ActionResult CreatOrder(string currentOrderDescription)
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

                // Tạo đơn hàng mới
                var objOrder = new Order
                {
                    Name = currentOrderDescription,
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


    }
}