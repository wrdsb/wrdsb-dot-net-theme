using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WRDSB_Dot_Net_Template
{
    public partial class aboutus : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetAllowResponseInBrowserHistory(false);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();

            if (this.Page.User.Identity.IsAuthenticated)
            {
            }
            else
            {
                Session.Clear();
                Session.Abandon();
                Session.RemoveAll();
                FormsAuthentication.RedirectToLoginPage();
            }
        }
    }
}