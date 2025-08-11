using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using VoPhanKhaHy_CDTT.Models;
using VoPhanKhaHy_CDTT.Context;


namespace VoPhanKhaHy_CDTT.Areas.Admin.Controllers
{
    public class OrderController : Controller
    {
        private WebAspDbEntities _context;
        public OrderController()
        {
            _context = new WebAspDbEntities();
        }
        // GET: Admin/Order
        //public ActionResult ListOrder(int page = 1, int pageSize = 6)
        //{
        //    // Tính tổng số đơn hàng
        //    int totalItems = _context.Orders.Count();

        //    // Lấy danh sách đơn hàng theo trang
        //    var listOrder = _context.Orders
        //                            .OrderByDescending(o => o.Id)
        //                            .Skip((page - 1) * pageSize)
        //                            .Take(pageSize)
        //                            .ToList();

        //    // Tính tổng số trang
        //    int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        //    // Lưu thông tin vào ViewBag
        //    ViewBag.CurrentPage = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.TotalPages = totalPages;

        //    return View(listOrder);
        //}
        public ActionResult ListOrder(int? page)
        {
            int pageSize = 5;
            int pageNumber = (page ?? 1);

            // Truy vấn và phân trang trực tiếp từ database
            var listOrder = _context.Orders
                .OrderByDescending(o => o.Id)
                .ToPagedList(pageNumber, pageSize);

            // Nếu là AJAX request, trả về PartialView
            if (Request.IsAjaxRequest())
            {
                return PartialView("_ListOrder", listOrder);
            }

            // Nếu không, trả về View
            return View(listOrder);
        }
        public ActionResult Details(int id)
        {
            // Lấy thông tin đơn hàng
            var order = _context.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return HttpNotFound(); // Xử lý nếu không tìm thấy đơn hàng
            }

            // Lấy thông tin người dùng
            var user = _context.Users.FirstOrDefault(u => u.Id == order.UserId);

            // Lấy danh sách chi tiết đơn hàng và sản phẩm
            var orderDetails = (from od in _context.OrderDetails
                                join p in _context.Products on od.ProductId equals p.Id
                                where od.OrderId == id
                                select new OrderDetailProductViewModel
                                {
                                    ProductName = p.Name,
                                    Quantity = od.Quantity,
                                    Price = od.Price,
                                    TotalPrice = od.TotalPrice
                                }).ToList();

            // Tạo ViewModel để truyền dữ liệu
            var viewModel = new OrderViewModel
            {
                Order = order,
                User = user,
                OrderDetails = orderDetails
            };

            return View(viewModel);
        }
        [HttpPost]
        public JsonResult UpdateStatus(int id, int status)
        {
            try
            {
                // Tìm đơn hàng theo Id
                var order = _context.Orders.FirstOrDefault(o => o.Id == id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Đơn hàng không tồn tại." });
                }

                // Cập nhật trạng thái
                order.Status = status;
                order.UpdatedAt = DateTime.Now;

                // Lưu thay đổi
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}