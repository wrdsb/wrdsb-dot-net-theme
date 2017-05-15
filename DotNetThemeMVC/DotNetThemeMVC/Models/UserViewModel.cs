using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DotNetThemeMVC.Models
{
    public class UserViewModel
    {
        public IPagedList<UserRoleViewModel> Users { get; set; }
        public string searchString { get; set; }
        public string roleFilter { get; set; }
        public int pageSize { get; set; }
        public int page { get; set; }
        public string sortOrder { get; set; }
    }
}