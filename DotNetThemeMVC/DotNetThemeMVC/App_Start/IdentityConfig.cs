using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using DotNetThemeMVC.Models;
using System.Net.Mail;
using System.Configuration;

namespace DotNetThemeMVC
{
    public class EmailService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            /*
            //Store the passed in body text
            string bodyText = message.Body;

            //Configure the email template
            //Subsitute new email text to the identity message message
            string emailTemplate = "<div id='email' style='display: block;'>" +
                                    "<div id='logo' style='display: block;margin-top: 14px;'>" +
                                    "<a href='https://www.wrdsb.ca' style='height: 150px;display: inline-block;width: 100%;background: url(https://s3.amazonaws.com/wrdsb-ui-assets/0/0.10.4/images/wrdsb_logo_medallion.gif) no-repeat;background-size: 150 136;padding-left: 160px;text-decoration: none;color: #000;'>" +
                                    "<h1 style='font-size:30px;font-weight:bold;margin:0;top:58px;position:absolute;color: #005daa;'>" + @System.Web.Configuration.WebConfigurationManager.AppSettings["loginTitle"].ToString() + "</h1>" +
                                    "</a>" +
                                    "</div>" +
                                    "<div id='greenbar' style='background-color:#7ac143; height:6px;'></div>" +
                                    "<br />" +
                                    "<div id='body' style='display: block;'>" +
                                    bodyText +
                                    "</div>" +
                                    "<div id='legal' style='display: block;'>" +
                                    "<p style='font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif;font-size: 12px;line-height: 130%;color: #333;background-color: #fff;margin: 0 10%;'>" +
                                    "Confidentiality Warning: ~This message and any attachments are intended only for the use of the intended recipient(s) and may contain confidential or personal information that may be subject to the provisions of the Municipal Freedom of Information and Protection of Privacy Act. ~If you are not the intended recipient or an authorized representative of the intended recipient, you are notified that any dissemination of this communication is strictly prohibited.~ If you have received this communication in error, please notify the sender immediately and delete the message and any attachments." +
                                    "</p></div></div>";

            message.Body = emailTemplate;

            // convert IdentityMessage to a MailMessage
            var email =
               new MailMessage(new MailAddress("noreply@wrdsb.ca", "WRDSB (do not reply)"),
               new MailAddress(message.Destination))
               {
                   Subject = message.Subject,
                   Body = message.Body,
                   IsBodyHtml = true
               };

            using (var client = new SmtpClient()) // SmtpClient configuration comes from config file
            {
                await client.SendMailAsync(email);
            }*/
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                //RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                //RequireNonLetterOrDigit = true,
                RequireDigit = true,
                //RequireLowercase = true,
                //RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}