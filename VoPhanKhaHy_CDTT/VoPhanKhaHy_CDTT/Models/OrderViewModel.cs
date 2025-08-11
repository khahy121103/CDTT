using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Models
{
    public class OrderViewModel
    {
        public Order Order { get; set; } // Thông tin đơn hàng
        public User User { get; set; }   // Thông tin người dùng
        public List<OrderDetailProductViewModel> OrderDetails { get; set; }
    }
    public class OrderDetailProductViewModel
    {
        public string ProductName { get; set; } // Tên sản phẩm
        public int Quantity { get; set; }      // Số lượng
        public double Price { get; set; }      // Giá
        public double TotalPrice { get; set; } // Tổng giá
    }
}