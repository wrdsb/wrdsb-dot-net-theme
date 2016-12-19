using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using DotNetThemeMVC.Models;
using System.Web.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;

namespace DotNetThemeMVC.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public bool AuthenticateAD(string username, string password)
        {
            using (var context = new PrincipalContext(ContextType.Domain, System.Web.Configuration.WebConfigurationManager.AppSettings["adAuthURL"].ToString(), username, password))
            {
                return context.ValidateCredentials(username, password);
            }
        }

        /// <summary>
        /// Compares authentication administrator against a control table of access granted administrators.
        /// </summary>
        /// <param name="username">The PAL username to verify</param>
        /// <returns>boolean</returns>
        //public bool CheckAdministrators(string username)
        //{
        //    administratorEntities db = new administratorEntities();
        //    var adminList = db.administrators.ToList();

        //    foreach (administrators admin in adminList)
        //    {
        //        if (admin.username.Equals(username))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        // POST: /Account/Login
        /// <summary>
        /// Authenticates the user against the db.
        /// </summary>
        /// <param name="model">The login object</param>
        /// <param name="returnUrl">the destination after login</param>
        /// <returns>View</returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            if (!model.Email.Contains("@"))
            {
                //Authenticate against AD and then assign local accounts
                if (AuthenticateAD(model.Email, model.Password))
                {
                    //Compare authenticated username against control administrator table
                    //This requires configuration. Create a control table in a SQL DB.
                    /* CREATE TABLE [dbo].[administrators](
	                    [id] [uniqueidentifier] NOT NULL,
	                    [username] [nvarchar](50) NOT NULL,
                        CONSTRAINT [PK_administrators] PRIMARY KEY CLUSTERED 
                        (
	                        [id] ASC
                            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                        ) ON [PRIMARY]
                     */
                    //When creating an ADO.Net model of the new table name it: administratorEntities
                    //if (CheckAdministrators(model.Email))
                    //{
                        //Check to see if a local account exists, if not create one. Set confirmedemail to true. Set Role to Administrators.
                        //if (UserManager.FindByName(model.Email) == null)
                        //{
                        //    var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                        //    var result = await UserManager.CreateAsync(user, model.Password);

                        //    //Check to see if the Administrator role exists, if not create one.
                        //    var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
                        //    if (roleManager.RoleExists("Administrators"))
                        //    {
                        //        var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                        //        role.Name = "Administrators";
                        //        roleManager.Create(role);
                        //    }
                        //    UserManager.AddToRole(user.Id, "Administrators");
                        //}
                    //}
                    //else
                    //{
                        //ModelState.AddModelError(string.Empty, "Not an authorized user.");
                        //return View(model);
                    //}
                }
                else
                {
                    ModelState.AddModelError("", "Login failed.");
                    return View(model);
                }
            }

            //Normal user login and routing
            var userid = UserManager.FindByEmail(model.Email).Id;
            if (!UserManager.IsEmailConfirmed(userid))
            {
                //Resend the code
                string callbackUrl = await SendEmailConfirmationTokenAsync(userid, "Confirm your account-Resend");
                return View("EmailNotConfirmed");
            }
            else
            {
                var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
                switch (result)
                {
                    case SignInStatus.Success:
                        //If signed in user is administrator take them to the administrator home page
                        if (UserManager.IsInRole(userid, "Administrators"))
                        {
                            return RedirectToAction("Home", "Administrator");
                        }
                        return RedirectToLocal(returnUrl);
                    case SignInStatus.LockedOut:
                        return View("Lockout");
                    case SignInStatus.RequiresVerification:
                        return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                    case SignInStatus.Failure:
                    default:
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(model);
                }
            }
        }

        /// <summary>
        /// Resends a confirmation code to the email the user provided
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        private async Task<string> SendEmailConfirmationTokenAsync(string userID, string subject)
        {
            string code = await UserManager.GenerateEmailConfirmationTokenAsync(userID);
            var callbackUrl = Url.Action("ConfirmEmail", "Account",
               new { userId = userID, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(userID, subject,
               "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

            return callbackUrl;
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                    string emailText = "<p style=\"font-size:2em;padding-top:1em;padding-bottom:1em;padding-right:1em;padding-left:1em;line-height:150%;text-align:center;background-color:#7ac143 ;margin-top:auto;margin-bottom:auto;margin-right:auto;margin-left:auto;\" >" +
                        "<a href=\"" + callbackUrl + "\" style=\"color:#fff;padding-top:1.3em;padding-bottom:1.3em;padding-right:1.3em;padding-left:1.3em;font-weight:bold;text-decoration:none;\" >Confirm my email address</a></p><br />" +
                        "Thank you for registering for French Immersion. In order to proceed please <a href=\"" + callbackUrl + "\">confirm</a> your account. " +
                        "If the link doesn't work copy and paste this link into your browser: " + callbackUrl + "<br />" +
                        "<h2>Next Steps</h2><br />" +
                        "Log in and complete the registration form for your child(ren).";

                    await UserManager.SendEmailAsync(user.Id, "Confirm your WRDSB French Immersion account",
                        emailText);

                    return View("EmailNotConfirmed");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                string emailText = "<p style=\"font-size:2em;padding-top:1em;padding-bottom:1em;padding-right:1em;padding-left:1em;line-height:150%;text-align:center;background-color:#7ac143 ;margin-top:auto;margin-bottom:auto;margin-right:auto;margin-left:auto;\" >" +
                    "<a href=\"" + callbackUrl + "\" style=\"color:#fff;padding-top:1.3em;padding-bottom:1.3em;padding-right:1.3em;padding-left:1.3em;font-weight:bold;text-decoration:none;\" >Reset my password</a></p><br />" +
                    "A request to <a href=\"" + callbackUrl + "\">reset</a> your password was initiated. In order to proceed please <a href=\"" + callbackUrl + "\">reset</a> your password." +
                    "If the link doesn't work copy and paste this link into your browser: " + callbackUrl +
                    "<br />If you did not request a password change ignore this email.";

                await UserManager.SendEmailAsync(user.Id, "Reset Password for WRDSB French Immersion account", emailText);
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}