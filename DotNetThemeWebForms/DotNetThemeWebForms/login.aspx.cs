using DotNetThemeWebForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DotNetTheme
{
    public partial class login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            String adPath = ""; //Fully-qualified Domain Name
            LdapAuthentication adAuth = new LdapAuthentication(adPath);
            try
            {
                if (true == adAuth.IsAuthenticated("ADMIN", txtUsername.Text, txtPassword.Text))
                { }
            }
            catch(Exception ex)
            {
                loginErrors.InnerHtml = "Authentication did not succeed. Check user name and password.";
                loginErrors.Style.Remove("visibility");
            }
        }
    }
}