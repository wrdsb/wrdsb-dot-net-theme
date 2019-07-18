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
using System.Configuration;

namespace DotNetThemeMVC.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
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

        ///This section is a work in progress, change to function being called from Login()
        /// <summary>
        /// Checks to see if the username belongs to a group(AD,IPPS,Trillium)
        /// See Documentation for enabling required Authorization: <a href=
        /// </summary>
        /// <param name="username">The username to verify</param>
        /// <returns>bool</returns>
        public bool IsMemberOf(string username)
        {
            //Read the AppSettings config file to see if any external authentication has been enabled
            string ADGroupsEnabled = System.Web.Configuration.WebConfigurationManager.AppSettings["adGroupAuth"].ToString();


            //If AD Groups is enabled execute the below code
            if (ADGroupsEnabled.Equals("enabled"))
            {
                //Get the list of approved Groups
                List<string> approvedGroups = db.ad_group_roles.Select(z => z.group_name).Distinct().ToList();

                ADProviderController ad = new ADProviderController();
                UserPrincipal user = ad.GetUserPrincipal(username);

                //Find out if the supplied username belongs to any Active Directory Group that has been authorized
                foreach (var approvedGroup in approvedGroups)
                {
                    GroupPrincipal group = ad.GetGroupPrincipal(approvedGroup);
                    if (user != null && group != null)
                    {
                        if (user.IsMemberOf(group))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            //Other Group Based Authorization can be written here(IPPS, Trillium) Future Versions
            //If IPPS is enabled execute the below code
            //If Trillium is enabled execute the below code
            return false;
        }

        /// <summary>
        /// Checks to see if an Account Exists for the supplied username
        /// </summary>
        /// <param name="username">The input value for username</param>
        /// <returns>boolean</returns>
        public bool accountExists(string username)
        {
            if (UserManager.FindByName(username) != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates an account for the supplied username
        /// </summary>
        /// <param name="username">The input value for username</param>
        public bool createAccount(string username)
        {
            ADProviderController ad = new ADProviderController();

            string email = ad.GetUserEmail(username);
            if(String.IsNullOrEmpty(email))
            {
                return false;
            }
            var user = new ApplicationUser { UserName = username, Email = email, EmailConfirmed = true };
            UserManager.Create(user);
            return true;
        }

        /// <summary>
        /// Checks to see if the user is in the Administrator Role
        /// </summary>
        /// <returns>boolean</returns>
        public bool isAdministrator(string username)
        {
            var user = UserManager.FindByName(username);

            if (UserManager.IsInRole(user.Id, "Administrators") || UserManager.IsInRole(user.Id, "SuperAdmin"))
            {
                return true;
            }
            return false;
        }

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
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, change to shouldLockout: true
                if (!model.Email.Contains("@"))
                {
                    ADProviderController ad = new ADProviderController();
                    //Authenticate against AD
                    if (ad.Authenticate(model.Email, model.Password))
                    {
                        //User has authenticated
                        //Check which Authorization method is enabled

                        //Set a flag, if user is Authorized set to true
                        bool userIsAuthorized = false;

                        //Authorize the user through AD Groups
                        if (ConfigurationManager.AppSettings["adGroupAuth"].Equals("true"))
                        {
                            //Get the status of membership and account existence
                            bool isGroupMember = IsMemberOf(model.Email);
                            bool accountExist = accountExists(model.Email);

                            //Is a member, account exists, authorized
                            if(isGroupMember && accountExist)
                            {
                                userIsAuthorized = true;
                            }
                            //Administrator accounts may not be in the authorized Active Directory group
                            //Check if the username  is an administrator
                            if(isAdministrator(model.Email))
                            {
                                userIsAuthorized = true;
                            }
                        }

                        //Authorize the user through IPPS
                        if (ConfigurationManager.AppSettings["ippsGroupAuth"].Equals("true"))
                        {
                            //Coming Soon
                            //Call a Function and return true/false
                            //userIsAuthorized = Function(model.Email);
                        }

                        //Authorize the user through Control Table(AspNetUsers)
                        if (ConfigurationManager.AppSettings["controlTableAuth"].Equals("true"))
                        {
                            //Call a Function and return true/false
                            userIsAuthorized = accountExists(model.Email);
                        }

                        //No Authorization
                        if (ConfigurationManager.AppSettings["noAuth"].Equals("true"))
                        {
                            //Check if local account exists
                            //Create if needed
                            if (!accountExists(model.Email))
                            {
                                var status = createAccount(model.Email);
                                if(!status)
                                {
                                    ModelState.AddModelError(model.Email, "Failed to retrieve email for username from Active Directory. Contact an administrator for help.");
                                    return View(model);
                                }
                            }

                            //Everyone is Authorized
                            userIsAuthorized = true;
                        }
                        if (userIsAuthorized)
                        {
                            ApplicationUser applicationUser = UserManager.FindByName(model.Email);
                            await SignInManager.SignInAsync(applicationUser, isPersistent: false, rememberBrowser: false);

                            //If they were linked to something inside the application send the user to that
                            if (Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }
                            //If Signed in user is in the role of Administrators or SuperAdmintake them to the administrator home page
                            else if (UserManager.IsInRole(applicationUser.Id, "Administrators") || UserManager.IsInRole(applicationUser.Id, "SuperAdmin"))
                            {
                                return RedirectToAction("Home", "Administrator");
                            }
                            else
                            {
                                return RedirectToAction("Index", "Home");
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
                }

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

        // GET: /Account/Register
        /// <summary>
        /// Displays the Register Index
        /// </summary>
        /// <returns>View</returns>
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (ConfigurationManager.AppSettings["isInternal"].Equals("false"))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        // POST: /Account/Register
        /// <summary>
        /// Creates a new account for the supplied credentials. Sends off a confirmation email.
        /// User cannot sign in until the link in the confirmation email is clicked.
        /// </summary>
        /// <param name="model">The register object</param>
        /// <returns>View</returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (ConfigurationManager.AppSettings["isInternal"].Equals("false"))
                    {
                        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                        var result = await UserManager.CreateAsync(user, model.Password);
                        if (result.Succeeded)
                        {
                            string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                            var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);

                            string emailText =
                                "<p style=\"font-size:2em;padding-top:1em;padding-bottom:1em;padding-right:1em;padding-left:1em;line-height:150%;text-align:center;background-color:#7ac143 ;margin-top:auto;margin-bottom:auto;margin-right:auto;margin-left:auto;\" >" +
                                "<a href=\"" + callbackUrl + "\" style=\"color:#fff;padding-top:1.3em;padding-bottom:1.3em;padding-right:1.3em;padding-left:1.3em;font-weight:bold;text-decoration:none;\" >Confirm my email address</a></p><br />" +
                                "<p style='font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif;font-size: 14px;line-height: 150%;color: #333;background-color: #fff;margin: 0 10%;'>Thank you for registering for " + System.Web.Configuration.WebConfigurationManager.AppSettings["title"].ToString() + ". In order to proceed please <a href=\"" + callbackUrl + "\">confirm</a> your account." +
                                "If the link doesn't work copy and paste this link into your browser: " + callbackUrl + "</p>" +
                                "<h2>Next Steps</h2>" +
                                "<p style='font-family: \"Helvetica Neue\", Helvetica, Arial, sans-serif;font-size: 14px;line-height: 150%;color: #333;background-color: #fff;margin: 0 10%;'>Log in and complete the registration form for your child(ren).</p>";
                            Email email = new Email();
                            email.SendEmail(user.Email, "Confirm your WRDSB " + System.Web.Configuration.WebConfigurationManager.AppSettings["title"].ToString() + " account : WRDSB", emailText);

                            return View("EmailNotConfirmed");
                        }
                        AddErrors(result);
                    }
                    else
                    {
                        return RedirectToAction("Login", "Account");
                    }
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

        // GET: /Account/ConfirmEmail
        /// <summary>
        /// Verifies that the link sent in the email came from the email requesting account access.
        /// </summary>
        /// <param name="userId">The userid of the new registering user</param>
        /// <param name="code">The code that was sent in the email</param>
        /// <returns></returns>
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

        // GET: /Account/ForgotPassword
        /// <summary>
        /// Displays the ForgotPassword view.
        /// </summary>
        /// <returns>View</returns>
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        /// <summary>
        /// Sends a link allowing the user to reset the password.
        /// </summary>
        /// <param name="model">The ForgotPassword object</param>
        /// <returns>View</returns>
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

        // GET: /Account/ForgotPasswordConfirmation
        /// <summary>
        /// Displays View saying an email was sent and to check their inbox
        /// </summary>
        /// <returns>View</returns>
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        /// <summary>
        /// Displays the Reset Password page
        /// </summary>
        /// <param name="code">Code to verify</param>
        /// <returns>View</returns>
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        // POST: /Account/ResetPassword
        /// <summary>
        /// Resets the password
        /// </summary>
        /// <param name="model">The ResetPassword object</param>
        /// <returns>View</returns>
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

        // GET: /Account/ResetPasswordConfirmation
        /// <summary>
        /// Displays the reset password confirmation
        /// </summary>
        /// <returns>View</returns>
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //POST: /Account/LogOff
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        /// <summary>
        /// Make a call to this to log off
        /// </summary>
        /// <returns>View</returns>
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