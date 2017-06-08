using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace DotNetThemeMVC.Models
{
    public class ADGroupRolesViewModel
    {
        public string groupName { get; set; }
        public List<string> groupRoles { get; set; }
        public List<string> allRoles { get; set; }
    }
}