using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using VoPhanKhaHy_CDTT.Models;
using System.Data.Entity;
using PagedList;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Areas.Admin.Controllers
{
    public class CategoryController : Controller
    {
        private WebAspDbEntities _context;
        public CategoryController()
        {
            _context = new WebAspDbEntities();
        }
        // GET: Admin/Category
        //public ActionResult ListCategory(int page = 1, int pageSize = 6)
        //{
        //    var listCategory = _context.Categories.Where(c => c.Deleted.HasValue && c.Deleted.Value == false)
        //                                .OrderByDescending(c => c.Id)
        //                                .Skip((page - 1) * pageSize)
        //                                .Take(pageSize)
        //                                .ToList();
        //    int totalItems = _context.Categories.Count(c => c.Deleted.HasValue && c.Deleted.Value == false);
        //    int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        //    if (Request.IsAjaxRequest())
        //    {
        //        return Json(new
        //        {
        //            categories = listCategory.Select(c => new
        //            {
        //                Id = c.Id,
        //                Name = c.Name,
        //                Image = c.Image,
        //                ShowOnHomePage = c.ShowOnHomePage,
        //                CreatedAt = c.CreatedAt?.ToString("dd/MM/yyyy"),
        //                UpdatedAt = c.UpdatedAt?.ToString("dd/MM/yyyy")
        //            }),
        //            currentPage = page,
        //            totalPages = totalPages
        //        }, JsonRequestBehavior.AllowGet);
        //    }

        //    // Lưu các giá trị vào ViewBag để sử dụng trong View
        //    ViewBag.CurrentPage = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.TotalPages = totalPages;
        //    return View(listCategory);
        //}
        public ActionResult ListCategory(int? page)
        {
            int pageSize = 5;
            int pageNumber = (page ?? 1);
            var listCategory = _context.Categories
               .Where(c => c.Deleted.HasValue && c.Deleted.Value == false)
               .OrderByDescending(c => c.Id)
               .ToPagedList(pageNumber, pageSize);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_ListCategory", listCategory);
            }

            return View(listCategory);
        }
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem có hình ảnh không
                if (image != null && image.ContentLength > 0)
                {
                    // Tạo tên tệp duy nhất cho hình ảnh
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                    // Đường dẫn lưu hình ảnh trong thư mục Images
                    var filePath = Path.Combine(Server.MapPath("~/Content/img/items/"), fileName);

                    // Lưu hình ảnh vào thư mục
                    image.SaveAs(filePath);

                    // Lưu tên tệp hình ảnh vào cơ sở dữ liệu
                    category.Image = fileName;
                }
                category.CreatedAt = DateTime.Now;
                category.UpdatedAt = null;
                category.Deleted = false;
                category.ShowOnHomePage = false;

                // Lưu sản phẩm vào cơ sở dữ liệu
                _context.Categories.Add(category);
                _context.SaveChanges();

                return RedirectToAction("ListCategory");
            }

            return View(category);
        }

        // Đảm bảo đóng DbContext khi controller bị hủy
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
        [HttpGet]
        public ActionResult Edit(int id)
        {
            // Tìm sản phẩm trong cơ sở dữ liệu
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Category category, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var existingCategory = _context.Categories.Find(category.Id);
                if (existingCategory == null)
                {
                    return HttpNotFound();
                }

                // Cập nhật thông tin sản phẩm
                existingCategory.Name = category.Name;
                existingCategory.DisplayOrder = category.DisplayOrder;
                existingCategory.ShowOnHomePage = category.ShowOnHomePage;
                existingCategory.UpdatedAt = DateTime.Now;

                // Nếu có ảnh mới, cập nhật ảnh
                if (image != null && image.ContentLength > 0)
                {
                    // Xóa ảnh cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(existingCategory.Image))
                    {
                        var oldPath = Path.Combine(Server.MapPath("~/Content/img/items"), existingCategory.Image);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Lưu ảnh mới
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Server.MapPath("~/Content/img/items"), fileName);
                    image.SaveAs(filePath);
                    existingCategory.Image = fileName;
                }

                _context.SaveChanges();
                TempData["Success"] = "Cập nhật thương hiệu thành công!";
                return RedirectToAction("ListCategory");
            }

            TempData["Error"] = "Có lỗi xảy ra khi cập nhật thương hiệu!";
            return View(category);
        }
        public ActionResult Delete(int id)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
            {
                return HttpNotFound();
            }

            // Xóa sản phẩm khỏi cơ sở dữ liệu
            _context.Categories.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = "Thương hiệu đã được xóa vĩnh viễn!";
            return RedirectToAction("ListCategory");
        }
        [HttpPost]
        public ActionResult UpdateShowOnHomePage(int id, bool showOnHomePage)
        {
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                category.ShowOnHomePage = showOnHomePage;
                _context.SaveChanges(); // Lưu thay đổi vào DB
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}