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
                var orderDetails = _context.OrderDetails
                                           .Where(od => od.OrderId == id)
                                           .Select(od => new OrderDetailDTO
                                           {
                                               ProductId = od.ProductId,
                                               Quantity = od.Quantity,
                                               Price = od.Price,
                                               TotalPrice = od.TotalPrice,
                                               CreatedAt = od.CreatedAt
                                           })
                                           .ToList();

                if (!orderDetails.Any())
                {
                    return Content("Không tìm thấy chi tiết đơn hàng.");
                }

                return PartialView("_OrderDetailsPartial", orderDetails);
            }
            catch (Exception ex)
            {
                return Content("Lỗi: " + ex.Message);
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