using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DotNetThemeMVC.Models
{
    public class RoleViewModels
    {
        public string id { get; set; }
        [Required(AllowEmptyStrings=false)]
        [Display(Name = "Name")]
        public string Name { get; set; }
    }
}
