using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Models
{
    public class HomeViewModel
    {
        public List<Product> Products { get; set; }
        public List<Brand> Brands { get; set; }
        public List<Category> Categories { get; set; }
        public List<User> Users { get; set; }
        public List<Order> Orders { get; set; }
    }
}