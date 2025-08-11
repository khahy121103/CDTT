using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using VoPhanKhaHy_CDTT.Context;
using VoPhanKhaHy_CDTT.Models;

namespace VoPhanKhaHy_CDTT.Areas.Admin.Controllers
{
    public class UserController : Controller
    {
        private WebAspDbEntities _context;
        public UserController()
        {
            _context = new WebAspDbEntities();
        }
        // GET: Admin/User
        //public ActionResult ListUser(int page = 1, int pageSize = 6)
        //{
        //    int totalItems = _context.Users.Count();

        //    var listUser = _context.Users
        //                            .OrderByDescending(u => u.Id)
        //                            .Skip((page - 1) * pageSize)
        //                            .Take(pageSize)
        //                            .ToList();

        //    // Tính tổng số trang
        //    int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        //    // Lưu thông tin vào ViewBag
        //    ViewBag.CurrentPage = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.TotalPages = totalPages;

        //    return View(listUser);
        //}
        public ActionResult ListUser(int? page)
        {
            int pageSize = 5;
            int pageNumber = (page ?? 1);

            // Truy vấn và phân trang trực tiếp từ database
            var listUser = _context.Users
                .OrderByDescending(u => u.Id)
                .ToPagedList(pageNumber, pageSize);

            // Nếu là AJAX request, trả về PartialView
            if (Request.IsAjaxRequest())
            {
                return PartialView("_listUser", listUser);
            }

            // Nếu không, trả về View
            return View(listUser);
        }
        public ActionResult Details(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        public ActionResult Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return HttpNotFound();
            }

            // Xóa sản phẩm khỏi cơ sở dữ liệu
            _context.Users.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = "Tài khoản đã được xóa vĩnh viễn!";
            return RedirectToAction("ListUser");
        }
    }
}