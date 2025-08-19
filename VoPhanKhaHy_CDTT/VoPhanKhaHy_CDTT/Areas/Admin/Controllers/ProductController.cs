using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using VoPhanKhaHy_CDTT.Context;
using VoPhanKhaHy_CDTT.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using OfficeOpenXml;
using OfficeOpenXml.Configuration;

namespace VoPhanKhaHy_CDTT.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {

        private WebAspDbEntities _context;
        // GET: Admin/Product
        public ProductController()
        {
            _context = new WebAspDbEntities();
        }

        public ActionResult Index()
        {
            var product = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .ToList();

            return View(product);
        }
        //public ActionResult ListProduct(int page = 1, int pageSize = 5)
        //{
        //    // Lấy danh sách sản phẩm chưa bị xóa
        //    var listProduct = _context.Products
        //                                .Where(p => p.Deleted.HasValue && p.Deleted.Value == false)
        //                                .OrderByDescending(p => p.Id) // Bạn có thể thay đổi theo nhu cầu
        //                                .Skip((page - 1) * pageSize)
        //                                .Take(pageSize)
        //                                .ToList();

        //    // Tính toán tổng số trang
        //    int totalItems = _context.Products.Count(p => p.Deleted.HasValue && p.Deleted.Value == false);
        //    int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        //    // Lưu các giá trị vào ViewBag để sử dụng trong View
        //    ViewBag.CurrentPage = page;
        //    ViewBag.PageSize = pageSize;
        //    ViewBag.TotalPages = totalPages;

        //    return View(listProduct);
        //}
        public ActionResult ListProduct(int? page)
        {
            int pageSize = 5; // Số sản phẩm mỗi trang
            int pageNumber = (page ?? 1); // Trang hiện tại, mặc định là 1

            // Truy vấn dữ liệu trực tiếp từ database với các điều kiện
            var listProduct = _context.Products
                .Where(p => p.Deleted.HasValue && p.Deleted.Value == false) // Lọc sản phẩm không bị xóa
                .OrderByDescending(p => p.Id) // Sắp xếp giảm dần theo ID
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList(); // Phân trang thủ công

            // Trả về PartialView nếu là AJAX request
            if (Request.IsAjaxRequest())
            {
                return PartialView("_ListProduct", listProduct);
            }

            // Trả về View cho non-AJAX request
            return View(listProduct);
        }

        [HttpGet]
        [ValidateInput(false)]
        public ActionResult Create()
        {
            var model = new ProductViewModel
            {
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList(),
                Brands = _context.Brands.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                }).ToList()
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel model, HttpPostedFileBase image)
        {
            // Kiểm tra giá khuyến mãi không được lớn hơn hoặc bằng giá gốc
            if (model.PriceDiscount.HasValue && model.PriceDiscount.Value >= model.Price)
            {
                ModelState.AddModelError("PriceDiscount", "Giá khuyến mãi phải nhỏ hơn giá gốc");
                
                // Tạo lại dropdown data khi có lỗi
                model.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                }).ToList();
                model.Brands = _context.Brands.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                }).ToList();
                
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Name = model.Name,
                    CategoryId = model.CategoryId,
                    BrandId = model.BrandId ?? 0,
                    ShortDes = model.ShortDes,
                    FullDescription = model.FullDescription,
                    Price = Convert.ToDouble(model.Price),
                    PriceDiscount = model.PriceDiscount.HasValue ? Convert.ToDouble(model.PriceDiscount) : (double?)null,
                    ShowOnHomePage = model.ShowOnHomePage ?? false,
                    TypeId = model.TypeId,
                    Slug = model.Slug,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null,   
                    Deleted = false
                };

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
                    product.Image = fileName;
                }

                // Lưu sản phẩm vào cơ sở dữ liệu
                _context.Products.Add(product);
                _context.SaveChanges();

                return RedirectToAction("ListProduct");
            }

            // Nếu có lỗi, trả về view với dữ liệu đã nhập
            model.Categories = _context.Categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();
            model.Brands = _context.Brands.Select(b => new SelectListItem
            {
                Value = b.Id.ToString(),
                Text = b.Name
            }).ToList();

            return View(model);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportExcel(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var fileExtension = Path.GetExtension(file.FileName);
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    TempData["Error"] = "Chỉ hỗ trợ tệp Excel (.xlsx, .xls)";
                    return RedirectToAction("ListProduct");
                }

                using (var package = new ExcelPackage(file.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var product = new Product
                        {
                            Name = worksheet.Cells[row, 1].Text,
                            Image = worksheet.Cells[row, 2].Text,
                            CategoryId = int.Parse(worksheet.Cells[row, 3].Text),
                            BrandId = int.Parse(worksheet.Cells[row, 4].Text),
                            ShortDes = worksheet.Cells[row, 5].Text,
                            ShowOnHomePage = worksheet.Cells[row, 6].Text.ToLower() == "yes",
                            FullDescription = worksheet.Cells[row, 7].Text,
                            Price = Convert.ToDouble(decimal.Parse(worksheet.Cells[row, 8].Text)),
                            PriceDiscount = Convert.ToDouble(decimal.Parse(worksheet.Cells[row, 9].Text)),
                            TypeId = int.Parse(worksheet.Cells[row, 10].Text),
                            Slug = worksheet.Cells[row, 11].Text,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            Deleted = worksheet.Cells[row, 12].Text.ToLower() == "yes"
                        };

                        _context.Products.Add(product);
                    }
                    _context.SaveChanges();
                }

                TempData["Success"] = "Nhập dữ liệu thành công!";
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn tệp Excel để tải lên.";
            }

            return RedirectToAction("ListProduct");
        }



        [HttpGet]
        public ActionResult ExportExcel()
        {
            try
            {
                // Lấy danh sách sản phẩm từ cơ sở dữ liệu
                var products = _context.Products.ToList();

                // EPPlus 8: Cần set LicenseContext trước khi dùng
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Products");

                    // Đặt tiêu đề cột
                    worksheet.Cells[1, 1].Value = "Name";
                    worksheet.Cells[1, 2].Value = "Image";
                    worksheet.Cells[1, 3].Value = "CategoryId";
                    worksheet.Cells[1, 4].Value = "BrandId";
                    worksheet.Cells[1, 5].Value = "Short Description";
                    worksheet.Cells[1, 6].Value = "Show On HomePage";
                    worksheet.Cells[1, 7].Value = "Full Description";
                    worksheet.Cells[1, 8].Value = "Price";
                    worksheet.Cells[1, 9].Value = "Price Discount";
                    worksheet.Cells[1, 10].Value = "TypeId";
                    worksheet.Cells[1, 11].Value = "Slug";
                    worksheet.Cells[1, 12].Value = "CreatedAt";
                    worksheet.Cells[1, 13].Value = "UpdatedAt";
                    worksheet.Cells[1, 14].Value = "Deleted";

                    // Điền dữ liệu sản phẩm vào các dòng
                    int row = 2;
                    foreach (var product in products)
                    {
                        worksheet.Cells[row, 1].Value = product.Name;
                        worksheet.Cells[row, 2].Value = product.Image;
                        worksheet.Cells[row, 3].Value = product.CategoryId;
                        worksheet.Cells[row, 4].Value = product.BrandId;
                        worksheet.Cells[row, 5].Value = product.ShortDes;
                        worksheet.Cells[row, 6].Value = (product.ShowOnHomePage ?? false) ? "Yes" : "No";
                        worksheet.Cells[row, 7].Value = product.FullDescription;
                        worksheet.Cells[row, 8].Value = product.Price;
                        worksheet.Cells[row, 9].Value = product.PriceDiscount;
                        worksheet.Cells[row, 10].Value = product.TypeId;
                        worksheet.Cells[row, 11].Value = product.Slug;
                        worksheet.Cells[row, 12].Value = product.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[row, 13].Value = product.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[row, 14].Value = (product.Deleted ?? false) ? "Yes" : "No";
                        row++;
                    }

                    // Định dạng bảng
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Xuất file Excel ra dạng byte
                    var fileContent = package.GetAsByteArray();

                    // Trả file về để người dùng tải xuống
                    return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Products.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xuất file: " + ex.Message;
                return RedirectToAction("ListProduct");
            }
        }



        [HttpGet]
        public ActionResult ExportPDF()
        {
            try
            {
                // Lấy danh sách sản phẩm từ cơ sở dữ liệu
                var products = _context.Products.ToList();

                // Tạo file PDF
                using (var stream = new MemoryStream())
                {
                    // Tạo document PDF
                    var document = new Document(PageSize.A4, 10, 10, 20, 20);
                    PdfWriter.GetInstance(document, stream);
                    document.Open();

                    // Thêm tiêu đề                  
                    var titleFont = iTextSharp.text.FontFactory.GetFont("Arial", 18, iTextSharp.text.Font.BOLD, BaseColor.BLUE);
                    var textFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.NORMAL);
                    var headerFont = iTextSharp.text.FontFactory.GetFont("Arial", 12, iTextSharp.text.Font.BOLD, BaseColor.WHITE);

                    var title = new Paragraph("Danh Sách Sản Phẩm\n\n", titleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };
                    document.Add(title);

                    // Tạo bảng PDF
                    var table = new PdfPTable(5) { WidthPercentage = 100 }; // 5 cột
                    table.SetWidths(new float[] { 2, 3, 2, 1, 1 }); // Đặt tỷ lệ chiều rộng cột

                    // Thêm tiêu đề các cột với màu nền
                    var headerBackgroundColor = new BaseColor(0, 102, 204); // Màu xanh
                    var headers = new[] { "Tên sản phẩm", "Hình ảnh", "Giá", "Danh mục", "Thương hiệu" };
                    foreach (var header in headers)
                    {
                        var cell = new PdfPCell(new Phrase(header, headerFont))
                        {
                            BackgroundColor = headerBackgroundColor,
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            Padding = 5
                        };
                        table.AddCell(cell);
                    }

                    // Điền dữ liệu vào bảng
                    foreach (var product in products)
                    {
                        table.AddCell(new PdfPCell(new Phrase(product.Name ?? "", textFont))
                        {
                            Padding = 5,
                            HorizontalAlignment = Element.ALIGN_LEFT
                        });
                        table.AddCell(new PdfPCell(new Phrase(product.Image ?? "", textFont))
                        {
                            Padding = 5,
                            HorizontalAlignment = Element.ALIGN_LEFT
                        });
                        table.AddCell(new PdfPCell(new Phrase(product.Price.ToString("C2"), textFont))
                        {
                            Padding = 5,
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });
                        table.AddCell(new PdfPCell(new Phrase(product.CategoryId.ToString(), textFont))
                        {
                            Padding = 5,
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        table.AddCell(new PdfPCell(new Phrase(product.BrandId.ToString(), textFont))
                        {
                            Padding = 5,
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                    }

                    document.Add(table);
                    document.Close();

                    // Trả file PDF về dưới dạng byte
                    var fileContent = stream.ToArray();
                    return File(fileContent, "application/pdf", "Products.pdf");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xuất file PDF: " + ex.Message;
                return RedirectToAction("ListProduct");
            }
        }




        [HttpGet]
        [ValidateInput(false)]
        public ActionResult Edit(int id)
        {
            // Tìm sản phẩm trong cơ sở dữ liệu
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Tạo ViewModel và ánh xạ dữ liệu
            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                Image = product.Image,
                ShortDes = product.ShortDes,
                FullDescription =product.FullDescription,
                Price = Convert.ToDecimal(product.Price), // Chuyển đổi từ double sang decimal
                PriceDiscount = product.PriceDiscount.HasValue ? Convert.ToDecimal(product.PriceDiscount) : (decimal?)null, // Chuyển đổi với kiểu nullable
                ShowOnHomePage = product.ShowOnHomePage ?? false,
                Slug = product.Slug,
                TypeId = product.TypeId,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == product.CategoryId // Chọn danh mục hiện tại
                }).ToList(),
                Brands = _context.Brands.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == product.BrandId // Chọn danh mục hiện tại
                }).ToList()
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Edit(Product product, HttpPostedFileBase image)
        {
            // Kiểm tra giá khuyến mãi không được lớn hơn hoặc bằng giá gốc
            if (product.PriceDiscount.HasValue && product.PriceDiscount.Value >= product.Price)
            {
                ModelState.AddModelError("PriceDiscount", "Giá khuyến mãi phải nhỏ hơn giá gốc");
                return View(product);
            }

            if (ModelState.IsValid)
            {
                var existingProduct = _context.Products.Find(product.Id);
                if (existingProduct == null)
                {
                    return HttpNotFound();
                }

                // Cập nhật thông tin sản phẩm
                existingProduct.Name = product.Name;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.ShortDes = product.ShortDes;
                existingProduct.FullDescription = product.FullDescription;
                existingProduct.Price = product.Price;
                existingProduct.PriceDiscount = product.PriceDiscount;
                existingProduct.ShowOnHomePage = product.ShowOnHomePage;
                existingProduct.TypeId = product.TypeId;
                existingProduct.UpdatedAt = DateTime.Now;

                // Nếu có ảnh mới, cập nhật ảnh
                if (image != null && image.ContentLength > 0)
                {
                    // Xóa ảnh cũ nếu tồn tại
                    if (!string.IsNullOrEmpty(existingProduct.Image))
                    {
                        var oldPath = Path.Combine(Server.MapPath("~/Content/img/items/"), existingProduct.Image);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Lưu ảnh mới
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                    var filePath = Path.Combine(Server.MapPath("~/Content/img/items/"), fileName);
                    image.SaveAs(filePath);
                    existingProduct.Image = fileName;
                }

                _context.SaveChanges();
                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("ListProduct");
            }

            TempData["Error"] = "Có lỗi xảy ra khi cập nhật sản phẩm!";
            return View(product);
        }
        public ActionResult Details(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Lấy thông tin danh mục và thương hiệu tương ứng
            var category = _context.Categories.FirstOrDefault(c => c.Id == product.CategoryId);
            var brand = _context.Brands.FirstOrDefault(b => b.Id == product.BrandId);

            // Truyền thông tin sản phẩm, danh mục và thương hiệu vào View
            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                Image = product.Image,
                ShortDes = product.ShortDes,
                FullDescription = product.FullDescription,
                Price = Convert.ToDecimal(product.Price), // Chuyển đổi từ double sang decimal
                PriceDiscount = product.PriceDiscount.HasValue ? Convert.ToDecimal(product.PriceDiscount) : (decimal?)null, // Chuyển đổi với kiểu nullable
                ShowOnHomePage = product.ShowOnHomePage,
                Slug = product.Slug,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                // Thêm tên danh mục và thương hiệu vào ViewModel
                CategoryName = category?.Name,
                BrandName = brand?.Name
            };

            return View(model);
        }
        public ActionResult Trash(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra xem sản phẩm có đang được sử dụng trong đơn hàng nào không
            var orderDetails = _context.OrderDetails.Where(od => od.ProductId == id).ToList();
            if (orderDetails.Any())
            {
                int orderCount = orderDetails.Select(od => od.OrderId).Distinct().Count();
                TempData["Error"] = $"Không thể xóa sản phẩm '{product.Name}' vì nó đang được sử dụng trong {orderCount} đơn hàng!";
                return RedirectToAction("ListProduct");
            }

            // Đánh dấu sản phẩm là đã xóa
            product.Deleted = true;
            _context.SaveChanges();

            TempData["Success"] = $"Sản phẩm '{product.Name}' đã được chuyển vào thùng rác!";
            return RedirectToAction("ListProduct");
        }

        [HttpGet]
        public ActionResult Restore(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Khôi phục sản phẩm
            product.Deleted = false;
            _context.SaveChanges();

            TempData["Success"] = "Sản phẩm đã được khôi phục!";
            return RedirectToAction("ListTrash");
        }
        public ActionResult ListTrash()
        {
            var deletedProducts = _context.Products
                                          .Where(p => p.Deleted.HasValue && p.Deleted.Value == true)
                                          .ToList();
            return View(deletedProducts);
        }
        [HttpGet]
        public ActionResult DeletePermanent(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Xóa sản phẩm khỏi cơ sở dữ liệu
            _context.Products.Remove(product);
            _context.SaveChanges();

            TempData["Success"] = "Sản phẩm đã được xóa vĩnh viễn!";
            return RedirectToAction("ListTrash");
        }

        [HttpPost]
        public ActionResult UpdateShowOnHomePage(int id, bool showOnHomePage)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                product.ShowOnHomePage = showOnHomePage;
                _context.SaveChanges(); // Lưu thay đổi vào DB
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public JsonResult RestoreAll()
        {
            try
            {
                var trashProducts = _context.Products.Where(p => p.Deleted.HasValue && p.Deleted.Value).ToList();

                foreach (var product in trashProducts)
                {
                    product.Deleted = false;
                }

                _context.SaveChanges();

                return Json(new { success = true, message = "Tất cả sản phẩm đã được khôi phục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi khôi phục tất cả sản phẩm: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DeleteAllPermanent()
        {
            try
            {
                var trashProducts = _context.Products.Where(p => p.Deleted.HasValue && p.Deleted.Value).ToList();

                if (trashProducts.Any())
                {
                    _context.Products.RemoveRange(trashProducts);
                    _context.SaveChanges();

                    return Json(new { success = true, message = "Tất cả sản phẩm trong thùng rác đã được xóa vĩnh viễn!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào để xóa!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tất cả sản phẩm: " + ex.Message });
            }
        }

    }
}