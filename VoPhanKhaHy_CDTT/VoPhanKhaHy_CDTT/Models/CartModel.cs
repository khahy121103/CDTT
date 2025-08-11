using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Models
{
    public class CartModel
    {
        public Product Product { get; set; }
        public int Quantity { get; set; }
    }
}