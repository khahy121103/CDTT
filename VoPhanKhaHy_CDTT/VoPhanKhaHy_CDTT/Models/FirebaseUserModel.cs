using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VoPhanKhaHy_CDTT.Models
{
    public class FirebaseUserModel
    {
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Uid { get; set; }
        public string Provider { get; set; }
    }
}