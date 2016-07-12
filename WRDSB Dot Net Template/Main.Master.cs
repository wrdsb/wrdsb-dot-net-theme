using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Security;

namespace WRDSB_Dot_Net_Template
{
    public partial class Main : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            lbl_username.Text = Context.User.Identity.Name;
            lbl_username2.Text = Context.User.Identity.Name;
            //lbl_groups.Text = Session["sub_job_description"].ToString();
            lbl_version.Text = System.Configuration.ConfigurationManager.AppSettings["version"].ToString();

            SetCurrentPage();
        }

        protected void lb_logout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Session.RemoveAll();

            FormsAuthentication.SignOut();
            FormsAuthentication.RedirectToLoginPage();
        }
        private void SetCurrentPage()
        {
            var pageName = GetPageName();

            switch (pageName)
            {
                case "default.aspx":
                    home_link.Attributes["class"] = "active";
                    break;
                case "aboutus.aspx":
                    leaves_link.Attributes["class"] = "active";
                    break;
                default:
                    break;
            }
        }
        private string GetPageName()
        {
            return Request.Url.ToString().Split('/').Last();
        }
    }
}