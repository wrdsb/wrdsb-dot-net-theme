using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DotNetThemeMVC.Models
{
    public class UserRoleViewModel
    {
        public ApplicationUser User { get; set; }
        public IList<string> userRoles { get; set; }
        public List<string> allRoles { get; set; }
    }
}