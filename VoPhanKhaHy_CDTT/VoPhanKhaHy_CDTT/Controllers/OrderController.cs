using Google;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Controllers
{
    public class OrderController : Controller
    {
        WebAspDbEntities db = new WebAspDbEntities();

        // GET: Order
        public ActionResult Index()
        {
            // Kiểm tra xem user đã đăng nhập hay chưa
            if (Session["idUser"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            // Lấy ID user từ session
            int userId = (int)Session["idUser"];

            // Lấy danh sách đơn hàng của user
            var orders = db.Orders.Where(o => o.UserId == userId).ToList();

            // Lưu số lượng đơn hàng vào session
            Session["odercount"] = orders.Count;

            return View(orders);
        }

        private readonly WebAspDbEntities _context;

        // Constructor
        public OrderController()
        {
            _context = new WebAspDbEntities(); // Khởi tạo đối tượng DbContext
        }
        [HttpGet]
        public ActionResult GetOrderDetails(int id)
        {
            try
            {
                // Kiểm tra đăng nhập
                if (Session["idUser"] == null)
                {
                    return Json(new { error = "Bạn cần đăng nhập để xem chi tiết đơn hàng." }, JsonRequestBehavior.AllowGet);
                }

                int userId = (int)Session["idUser"];

                // Kiểm tra đơn hàng có thuộc về user hiện tại không
                var order = _context.Orders.FirstOrDefault(o => o.Id == id && o.UserId == userId);
                if (order == null)
                {
                    return Json(new { error = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn hàng này." }, JsonRequestBehavior.AllowGet);
                }

                // Lấy chi tiết đơn hàng với thông tin sản phẩm
                var orderDetails = (from od in _context.OrderDetails
                                   join p in _context.Products on od.ProductId equals p.Id
                                   where od.OrderId == id
                                   select new OrderDetailDTO
                                   {
                                       ProductId = od.ProductId,
                                       ProductName = p.Name,
                                       ProductImage = p.Image,
                                       Quantity = od.Quantity,
                                       Price = od.Price,
                                       TotalPrice = od.TotalPrice,
                                       CreatedAt = od.CreatedAt
                                   }).ToList();

                if (!orderDetails.Any())
                {
                    return Json(new { error = "Không tìm thấy chi tiết đơn hàng." }, JsonRequestBehavior.AllowGet);
                }

                return Json(orderDetails, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Lỗi: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetOrderCount()
        {
            if (Session["idUser"] == null)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }

            int userId = (int)Session["idUser"];
            int count = db.Orders.Count(o => o.UserId == userId);

            return Json(new { count }, JsonRequestBehavior.AllowGet);
        }
    }
}