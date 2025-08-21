using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Controllers
{
    public class ProductController : Controller
    {
        WebAspDbEntities objWebAspDbEntities = new WebAspDbEntities();

        // GET: Product/Detail/Id
        public ActionResult Detail(int Id)
        {
            var objProduct = objWebAspDbEntities.Products.FirstOrDefault(n => n.Id == Id);
            return View(objProduct);
        }

        // GET: Product/FilterByPrice?sortOrder=asc  hoặc  Product/FilterByPrice?sortOrder=desc
        public ActionResult FilterByPrice(string sortOrder)
        {
            var products = objWebAspDbEntities.Products.AsQueryable();

            switch (sortOrder)
            {
                case "asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                default:
                    // mặc định sắp xếp theo Id (hoặc không sắp xếp)
                    products = products.OrderBy(p => p.Id);
                    break;
            }

            return PartialView("_ProductListPartial", products.ToList());
        }
    }
}
