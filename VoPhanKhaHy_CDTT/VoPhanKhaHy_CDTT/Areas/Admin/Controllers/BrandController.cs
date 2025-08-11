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
    public class BrandController : Controller
    {
        private WebAspDbEntities _context;
        public BrandController()
        {
            _context = new WebAspDbEntities();
        }
        // GET: Admin/Brand
        //public ActionResult ListBrand(int page = 1, int pageSize = 6)
        //{
        //    var listBrand = _context.Brands.Where(b => b.Deleted.HasValue && b.Deleted.Value == false)
        //                                .OrderByDescending(b => b.Id)
        //                                .Skip((page - 1) * pageSize)
        //                                .Take(pageSize)
        //                                .ToList();
        //    int totalItems = _context.Brands.Count(b => b.Deleted.HasValue && b.Deleted.Value == false);
        //    int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        //    // Lưu các giá trị vào ViewBag để sử dụng trong View
        //    ViewBag.CurrentPage = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.TotalPages = totalPages;
        //    return View(listBrand);
        //}
        public ActionResult ListBrand(int? page)
        {
            int pageSize = 5;
            int pageNumber = (page ?? 1);

            // Truy vấn và phân trang trực tiếp từ database
            var listBrand = _context.Brands
                .Where(b => b.Deleted.HasValue && b.Deleted.Value == false)
                .OrderByDescending(b => b.Id)
                .ToPagedList(pageNumber, pageSize);

            // Nếu là AJAX request, trả về PartialView
            if (Request.IsAjaxRequest())
            {
                return PartialView("_ListBrand", listBrand);
            }

            // Nếu không, trả về View
            return View(listBrand);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Brand brand, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem có hình ảnh không
                if (image != null && image.ContentLength > 0)
                {
                    // Tạo tên tệp duy nhất cho hình ảnh
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

                    // Đường dẫn lưu hình ảnh trong thư mục Images
                    var filePath = Path.Combine(Server.MapPath("~/Content/img/items"), fileName);

                    // Lưu hình ảnh vào thư mục
                    image.SaveAs(filePath);

                    // Lưu tên tệp hình ảnh vào cơ sở dữ liệu
                    brand.Image = fileName;
                }
                brand.CreatedAt = DateTime.Now;
                brand.UpdatedAt = null;
                brand.Deleted = false;
                brand.ShowOnHomePage = false;

                // Lưu sản phẩm vào cơ sở dữ liệu
                _context.Brands.Add(brand);
                _context.SaveChanges();

                return RedirectToAction("ListBrand");
            }

            return View(brand);
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
            var brand = _context.Brands.Find(id);
            if (brand == null)
            {
                return HttpNotFound();
            }

            return View(brand);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Brand brand, HttpPostedFileBase image)
        {
            if (ModelState.IsValid)
            {
                var existingBrand = _context.Brands.Find(brand.Id);
                if (existingBrand == null)
                {
                    return HttpNotFound();
                }

                // Cập nhật thông tin sản phẩm
                existingBrand.Name = brand.Name;
                existingBrand.DisplayOrder = brand.DisplayOrder;
                existingBrand.ShowOnHomePage = brand.ShowOnHomePage;
                existingBrand.UpdatedAt = DateTime.Now;

                // Nếu có ảnh mới, cập nhật ảnh
                if (image != null && image.ContentLength > 0)
                {
                    // Xóa ảnh cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(existingBrand.Image))
                    {
                        var oldPath = Path.Combine(Server.MapPath("~/Content/img/items"), existingBrand.Image);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Lưu ảnh mới
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Server.MapPath("~/Content/img/items"), fileName);
                    image.SaveAs(filePath);
                    existingBrand.Image = fileName;
                }

                _context.SaveChanges();
                TempData["Success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction("ListBrand");
            }

            TempData["Error"] = "Có lỗi xảy ra khi cập nhật danh mục!";
            return View(brand);
        }
        public ActionResult Delete(int id)
        {
            var brand = _context.Brands.FirstOrDefault(b => b.Id == id);
            if (brand == null)
            {
                return HttpNotFound();
            }

            // Xóa sản phẩm khỏi cơ sở dữ liệu
            _context.Brands.Remove(brand);
            _context.SaveChanges();

            TempData["Success"] = "Danh mục đã được xóa vĩnh viễn!";
            return RedirectToAction("ListBrand");
        }
        [HttpPost]
        public JsonResult UpdateShowOnHomePage(int id, bool showOnHomePage)
        {
            try
            {
                var brand = _context.Brands.Find(id);
                if (brand != null)
                {
                    brand.ShowOnHomePage = showOnHomePage;
                    _context.SaveChanges();

                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

    }
}