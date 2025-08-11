using Google;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoPhanKhaHy_CDTT.Models;
using VoPhanKhaHy_CDTT.Context;


namespace VoPhanKhaHy_CDTT.Controllers
{
    public class CategoryController : Controller
    {
        WebAspDbEntities objWebAspDbEntities = new WebAspDbEntities();
        // GET: Category
        public ActionResult Index()
        {
            HomeModel objHomeModel = new HomeModel();
            objHomeModel.ListCategory = objWebAspDbEntities.Categories.ToList();
            return View(objHomeModel);
        }
    }
}