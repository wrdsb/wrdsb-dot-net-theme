using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WRDSB_Dot_Net_Template
{
    public partial class login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            tb_username.Focus();
        }

        protected void btn_login_Click(object sender, EventArgs e)
        {
            String adPath = "LDAP://ec-dc1.wrdsb.ca"; //Fully-qualified Domain Name
            LdapAuthentication adAuth = new LdapAuthentication(adPath);
            try
            {
                if (true == adAuth.IsAuthenticated("ADMIN", tb_username.Text.ToLower(), tb_password.Text))
                {
                    String groups = adAuth.GetGroups(); //member of groups
                    string dept = adAuth.GetDepartment(); //school code
                    string surname = adAuth.GetSurname();
                    string firstname = adAuth.GetFirstname();
                    string job_desc = adAuth.GetDescription(); //job title
                    string ein = adAuth.GetEIN();
                    string email = adAuth.GetEmail();
                    string emp_group_code = adAuth.GetGroupCode();
                    //string job_description = "";
                    job_desc = job_desc.Substring(4).Trim();

                    Session["surname"] = surname;
                    Session["firstname"] = firstname;
                    Session["groups"] = groups;
                    Session["dept"] = dept;
                    Session["job_desc"] = job_desc;
                    Session["ein"] = ein;
                    Session["email"] = email;
                    Session["group_code"] = emp_group_code;
                    //Create the ticket, and add the groups.
                    //bool isCookiePersistent = cb_persist.Checked;
                    bool isCookiePersistent = false;
                    System.Web.Configuration.AuthenticationSection authSection = (System.Web.Configuration.AuthenticationSection)ConfigurationManager.GetSection("system.web/authentication");

                    System.Web.Configuration.FormsAuthenticationConfiguration
                        formsAuthenticationSection = authSection.Forms;

                    DateTime now = DateTime.Now;

                    FormsAuthenticationTicket authTicket = new FormsAuthenticationTicket(1, tb_username.Text,
                        now, now.Add(formsAuthenticationSection.Timeout), isCookiePersistent, "groups");

                    //Encrypt the ticket.
                    String encryptedTicket = FormsAuthentication.Encrypt(authTicket);

                    //Create a cookie, and then add the encrypted ticket to the cookie as data.
                    HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);

                    if (true == isCookiePersistent)
                        authCookie.Expires = authTicket.Expiration;

                    //Add the cookie to the outgoing cookies collection.
                    Response.Cookies.Add(authCookie);

                    //You can redirect now.
                    //Session["authenticated"] = true;
                    Response.Redirect(FormsAuthentication.GetRedirectUrl(tb_username.Text.ToLower(), false));
                }
                else
                {
                    lbl_message.Text = "Authentication did not succeed. Check user name and password.";
                    lbl_message.CssClass = "red";
                }
            }

            catch (Exception ex)
            {
                lbl_message.Text = "Error authenticating. " + ex.Message;
                lbl_message.CssClass = "red";
            }
        }
    }
}