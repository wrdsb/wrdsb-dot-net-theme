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
        /*
        /// <summary>
        /// Authenticates an administrator against active directory.
        /// </summary>
        /// <param name="username">The input value for username</param>
        /// <param name="password">The input value for password</param>
        /// <returns>boolean</returns>
        public bool AuthenticateAD(string username, string password)
        {
            using (var context = new PrincipalContext(ContextType.Domain, System.Web.Configuration.WebConfigurationManager.AppSettings["adAuthURL"].ToString(), username, password))
            {
                return context.ValidateCredentials(username, password);
            }
        }

        /// <summary>
        /// Compares authentication board user against a control table of access granted board users
        /// </summary>
        /// <param name="username">The PAL username to verify</param>
        /// <returns>boolean</returns>
        public bool AuthorizedUser(string username)
        {
            boardUsersEntities db = new boardUsersEntities();
            var userList = db.board_users.ToList();

            foreach (board_users user in userList)
            {
                if (user.username.Equals(username))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the role for a specified username
        /// </summary>
        /// <param name="username">The PAL username to lookup</param>
        /// <returns>string</returns>
        public string GetUserRole(string username)
        {
            boardUsersEntities db = new boardUsersEntities();
            board_users user = db.board_users.Where(x => x.username == username).FirstOrDefault();
            return user.role;
        }*/

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
            try
            {
                //This if block contains all code for authenticating a board user
                //1)Uncomment this and the functions it calls to authenticate board users
                //Functions:
                //AuthenticateAD
                //AuthorizedUser
                //GetUserRole
                //2)Uncomment all lines in AdministratorController, BoardUsersController, and RoleController
                /*
                if (!model.Email.Contains("@"))
                {
                    //Authenticate against AD and then assign local accounts
                    if (AuthenticateAD(model.Email, model.Password))
                    {
                        //Compare authenticated username against control administrator table
                        if (AuthorizedUser(model.Email))
                        {
                            //Get the role that was assigned to the user by the administrator
                            var userRole = GetUserRole(model.Email);

                            //1)This requires configuration. Create a control table in a SQL DB.
                            // CREATE TABLE [dbo].[board_users](
	                        //    [id] [uniqueidentifier] NOT NULL,
	                        //    [username] [nvarchar](50) NOT NULL,
	                        //    [role] [nvarchar](50) NOT NULL,
                            //    CONSTRAINT [PK_board_users_1] PRIMARY KEY CLUSTERED 
                            //    (
	                        //        [id] ASC
                            //        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                            //    ) ON [PRIMARY]
                            
                            //2)Insert yourself into board_users, substitute your PAL into the second parameter:
                            //Insert INTO board_users(id, username,role) VALUES(NEWID(), '', 'SuperAdmin')
                            //3)Insert yourself into AspNetUsers, substitute your email into the second parameter, PAL for the last parameter:
                            //INSERT INTO AspNetUsers VALUES(NEWID(),'',1 ,NULL,NEWID(),NULL,0,0,NULL,1,0,'')
                            //4)Insert a role into AspNetRoles:
                            //INSERT INTO AspNetRoles VALUES(NEWID(),'SuperAdmin')
                            //5)Insert a user role into AspNetUserRoles, substitute the id created in step 3 into the first parameter, substitute the id created in step 4 into the second parameter:
                            //INSERT INTO AspNetUserRoles VALUES('','')
                            //6)When creating an ADO.Net model of the new table name it: boardUserEntities

                            //Creates the Role in the AspNetRoles table only for SuperAdmins
                            if (userRole.Equals("SuperAdmin"))
                            {
                                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
                                if (roleManager.RoleExists(userRole) == false)
                                {
                                    var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                                    role.Name = userRole;
                                    roleManager.Create(role);
                                }
                            }

                            ApplicationUser applicationUser = UserManager.FindByName(model.Email);
                            await SignInManager.SignInAsync(applicationUser, isPersistent: false, rememberBrowser: false);

                            //If they were linked to something inside the application send the user to that
                            if (Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }
                            //If Signed in user is an administrator take them to the administrator home page
                            else if (userRole.Equals("Administrators") || userRole.Equals("SuperAdmin"))
                            {
                                return RedirectToAction("Home", "Administrator");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError(model.Email, "Not an authorized user.");
                            return View(model);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Login failed.");
                        return View(model);
                    }
                }*/

                //Normal user login and routing
                var userid = UserManager.FindByEmail(model.Email).Id;
                if (!UserManager.IsEmailConfirmed(userid))
                {
                    //Resend the code
                    string callbackUrl = await SendEmailConfirmationTokenAsync(userid, "Confirm your WRDSB " + System.Web.Configuration.WebConfigurationManager.AppSettings["title"].ToString() + "  account : WRDSB", model.Email);
                    return View("EmailNotConfirmed");
                }
                else
                {
                    var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
                    switch (result)
                    {
                        case SignInStatus.Success:
                            return RedirectToLocal(returnUrl);
                        case SignInStatus.LockedOut:
                            return View("Lockout");
                        case SignInStatus.RequiresVerification:
                            return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                        case SignInStatus.Failure:
                        default:
                            ModelState.AddModelError("", "Log in failed.");
                            return View(model);
                    }
                }
            }
            catch (NullReferenceException e)
            {
                //When an account doesnt exist the code: var userid = UserManager.FindByEmail(model.Email).Id; returns null
                //Handle the error by redirecting to log in page, do not log this error to a db or emailing the developers
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to sign you in. We are aware of the issue and will investigate. Please try signing in again. If the issue continues contact " + System.Web.Configuration.WebConfigurationManager.AppSettings["feedbackEmail"].ToString());
                return View(model);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Login.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to sign you in. We are aware of the issue and will investigate. Please try signing in again. If the issue continues contact " + System.Web.Configuration.WebConfigurationManager.AppSettings["feedbackEmail"].ToString());
                return View(model);
            }
        }

        /// <summary>
        /// Resends a confirmation code to the email the user provided
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="subject"></param>
        /// <returns></returns>
        private async Task<string> SendEmailConfirmationTokenAsync(string userID, string subject, string to)
        {
            string code = await UserManager.GenerateEmailConfirmationTokenAsync(userID);
            var callbackUrl = Url.Action("ConfirmEmail", "Account",
               new { userId = userID, code = code }, protocol: Request.Url.Scheme);

            string message = "Please <a href=\"" + callbackUrl + "\">confirm</a> your account. If the link doesn't work copy and paste this url into a browser: " + callbackUrl;
            Email email = new Email();
            email.SendEmail(to, subject, message);

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
            try
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
                            "Thank you for registering for " + System.Web.Configuration.WebConfigurationManager.AppSettings["title"].ToString() + ". In order to proceed please <a href=\"" + callbackUrl + "\">confirm</a> your account. " +
                            "If the link doesn't work copy and paste this link into your browser: " + callbackUrl + "<br />" +
                            "<h2>Next Steps</h2><br />" +
                            "Log in and complete the registration form for your child(ren).";
                        Email email = new Email();
                        email.SendEmail(user.Email, "Confirm your WRDSB " + System.Web.Configuration.WebConfigurationManager.AppSettings["title"].ToString() + " account : WRDSB", emailText);

                        return View("EmailNotConfirmed");
                    }
                    AddErrors(result);
                }

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Registration.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to create an account. We are aware of the issue and will investigate. Please try creating an account again. If the issue continues contact " + System.Web.Configuration.WebConfigurationManager.AppSettings["feedbackEmail"].ToString());
                return View(model);
            }
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

                Email email = new Email();
                email.SendEmail(model.Email, "Reset Password for WRDSB " + System.Web.Configuration.WebConfigurationManager.AppSettings["title"].ToString() + " account : WRDSB", emailText);
                
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