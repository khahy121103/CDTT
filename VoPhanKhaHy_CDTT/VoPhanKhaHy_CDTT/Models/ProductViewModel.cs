using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VoPhanKhaHy_CDTT.Context;

namespace VoPhanKhaHy_CDTT.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? TypeId { get; set; }
        public string CategoryName { get; set; }  // Thêm thuộc tính CategoryName
        public string BrandName { get; set; }     // Thêm thuộc tính BrandName
        public string Image { get; set; }
        public string ShortDes { get; set; }
        public string FullDescription { get; set; }
        public decimal Price { get; set; }
        public decimal? PriceDiscount { get; set; }
        public bool? ShowOnHomePage { get; set; }
        public string Slug { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Brands { get; set; }
    }
}