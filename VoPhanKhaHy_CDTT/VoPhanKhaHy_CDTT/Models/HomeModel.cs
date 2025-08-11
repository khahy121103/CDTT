using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Models
{
    public class HomeModel
    {
        public List<Product> ListProduct { get; set; }
        public List<Category> ListCategory { get; set; }
        public List<Brand> ListBrand { get; set; }
    }
}