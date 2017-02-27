using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RestSharp;
using RestSharp.Authenticators;

namespace DotNetThemeMVC.Controllers
{
    public class Email
    {
        /// <summary>
        /// Sends a POST to the mailgun API which sends out email
        /// </summary>
        /// <param name="to">The recipient of the email</param>
        /// <param name="subject">The subject line of the email</param>
        /// <param name="message">The body content of the email, html encoding accepted</param>
        public void SendEmail(string to, string subject, string message)
        {
            try
            {
                //Configure the email template
                //Plug in the passed in message into the template
                string emailMessage = "<div id='email' style='display: block;'>" +
                                        "<div id='logo' style='display: block;margin-top: 14px;'>" +
                                        "<a href='https://www.wrdsb.ca' style='height: 150px;display: inline-block;width: 100%;background: url(https://s3.amazonaws.com/wrdsb-ui-assets/0/0.10.4/images/wrdsb_logo_medallion.gif) no-repeat;background-size: 150 136;padding-left: 160px;text-decoration: none;color: #000;'>" +
                                        "<h1 style='font-size:30px;font-weight:bold;margin:0;top:58px;position:absolute;color: #005daa;'>" + @System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString() + "</h1>" +
                                        "</a>" +
                                        "</div>" +
                                        "<div id='greenbar' style='background-color:#7ac143; height:6px;'></div>" +
                                        "<br />" +
                                        "<div id='body' style='display: block;'>" +
                                        message +
                                        "</div>" +
                                        "<div id='legal' style='display: block;'>" +
                                        "<p style='font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif;font-size: 12px;line-height: 130%;color: #333;background-color: #fff;margin: 0 10%;'>" +
                                        "Confidentiality Warning: ~This message and any attachments are intended only for the use of the intended recipient(s) and may contain confidential or personal information that may be subject to the provisions of the Municipal Freedom of Information and Protection of Privacy Act. ~If you are not the intended recipient or an authorized representative of the intended recipient, you are notified that any dissemination of this communication is strictly prohibited.~ If you have received this communication in error, please notify the sender immediately and delete the message and any attachments." +
                                        "</p></div></div>";

                //For more information on configuring this code see the c# api documentation
                //https://documentation.mailgun.com/api-sending.html#examples
                RestClient client = new RestClient();
                client.BaseUrl = new Uri("https://api.mailgun.net/v3");
                client.Authenticator = new HttpBasicAuthenticator("api", "GET VALUE FROM MAILGUN");

                RestRequest request = new RestRequest();
                request.AddParameter("domain", "GET VALUE FROM MAILGUN", ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", "WRDSB (do not reply) <noreply@wrdsb.ca>");
                request.AddParameter("to", to);
                request.AddParameter("subject", subject);
                request.AddParameter("html", emailMessage);
                request.Method = Method.POST;
                client.Execute(request);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured attempting to send email.");
            }
        }
    }
}