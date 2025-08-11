using Google;
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
        // GET: Product
        public ActionResult Detail(int Id)
        {
            var objProduct = objWebAspDbEntities.Products.Where(n => n.Id == Id).FirstOrDefault();
            return View(objProduct);
        }
    }
}