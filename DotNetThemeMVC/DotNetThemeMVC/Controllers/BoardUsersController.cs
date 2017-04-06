using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DotNetThemeMVC.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using System.Web.Security;
using Microsoft.AspNet.Identity.EntityFramework;
using System.DirectoryServices.AccountManagement;
using DotNetThemeMVC;
using PagedList;

namespace WebApplication1.Controllers
{
    public class BoardUsersController : Controller
    {
        /*
        private boardUsersEntities db = new boardUsersEntities();

        private ApplicationUserManager _userManager;
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

        /// <summary>
        /// Returns the administrators email address from AD
        /// </summary>
        /// <param name="username">The input value for username</param>
        /// <returns>string</returns>
        public string GetADEmail(string username)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, System.Web.Configuration.WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            UserPrincipal user = UserPrincipal.FindByIdentity(context, username);
            if (user == null)
            {
                return "";
            }
            return user.EmailAddress;
        }

        /// <summary>
        /// Checks to see if the currently signed in user is in the Administrator Role
        /// </summary>
        /// <returns>bool</returns>
        public bool isAdministrator()
        {
            var userId = User.Identity.GetUserId();
            if (UserManager.IsInRole(userId, "Administrators"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the list of roles based on admin or superadmin status
        /// </summary>
        /// <returns>List<string></returns>
        public List<string> getRoles()
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            List<string> roles = roleManager.Roles.Select(x => x.Name).ToList();
            if (isAdministrator())
            {
                roles.Remove("SuperAdmin");
                return roles;
            }
            else
            {
                return roles;
            }

        }

        // GET: BoardUsers
        /// <summary>
        /// Displays the Board Users Index page listing all board users.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Index(int? page)
        {
            var pageNumber = page ?? 1;
            if (isAdministrator())
            {
                List<board_users> users = db.board_users.Where(x => x.role != "SuperAdmin").ToList();
                PagedList<board_users> model = new PagedList<board_users>(users, pageNumber, 10);

                return View(model);
            }
            else
            {
                //Else you are a SuperAdmin and see everyone
                PagedList<board_users> model = new PagedList<board_users>(db.board_users.ToList(), pageNumber, 10);
                return View(model);
            }
        }

        // GET: BoardUsers/Create
        /// <summary>
        /// Displays the Board Users Add page allowing administrator to add a board user to the db.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Create()
        {
            //The Viewbag for the role drop down needs to be set by who is signed in
            List<string> allRoles = getRoles();
            ViewBag.userRoles = new SelectList(allRoles);
            return View();
        }

        // POST: BoardUsers/Create
        /// <summary>
        /// Adds a board user to the db. Creates the Identity account and grants the selected role.
        /// </summary>
        /// <param name="board_users">The board users object</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Create([Bind(Include = "username,role")] board_users board_users)
        {
            if (ModelState.IsValid)
            {
                if(!getExistingUser(board_users.username))
                {
                    //Check to see if a local account exists, if not create one. Set ConfirmedEmail to true. Set Role.
                    if (UserManager.FindByName(board_users.username) == null)
                    {
                        var email = GetADEmail(board_users.username);

                        if (email.Equals(""))
                        {
                            ModelState.AddModelError(board_users.username, "Failed to retrieve email for username from Active Directory");
                            //The Viewbag for the role drop down needs to be set by who is signed in
                            List<string> allRoles = getRoles();
                            ViewBag.userRoles = new SelectList(allRoles);
                            return View(board_users);
                        }
                        var user = new ApplicationUser { UserName = board_users.username, Email = email, EmailConfirmed = true };
                        var result = UserManager.Create(user);

                        //Assign role to the user
                        UserManager.AddToRole(user.Id, board_users.role);
                    }

                    board_users.id = Guid.NewGuid();
                    db.board_users.Add(board_users);
                    db.SaveChanges();

                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(board_users.username, "This user exists.");
                    //The Viewbag for the role drop down needs to be set by who is signed in
                    List<string> allRoles = getRoles();
                    ViewBag.userRoles = new SelectList(allRoles);
                    return View(board_users);
                }
            }
            return View(board_users);
        }

        // GET: BoardUsers/Edit/5
        /// <summary>
        /// Displays the BoardUser Edit page allowing the administrator to edit an existing board users information.
        /// </summary>
        /// <param name="id">The id of the board user to edit</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            board_users board_users = db.board_users.Find(id);
            if (board_users == null)
            {
                return HttpNotFound();
            }
            //If an Administrator types into the url a SuperAdmins id, return not found
            if (board_users.role.Equals("SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }
            //The Viewbag for the role drop down needs to be set by who is signed in
            List<string> allRoles = getRoles();
            ViewBag.userRoles = new SelectList(allRoles);

            return View(board_users);
        }

        /// <summary>
        /// Get a username based on user id
        /// </summary>
        /// <param name="id">The id to look up</param>
        /// <returns>string</returns>
        public string getRecordUsername(Guid id)
        {
            boardUsersEntities db = new boardUsersEntities();
            return db.board_users.Where(x => x.id == id).FirstOrDefault().username;
        }

        /// <summary>
        /// Get a role based on user id
        /// </summary>
        /// <param name="id">The id to look up</param>
        /// <returns>string</returns>
        public string getRecordRole(Guid id)
        {
            boardUsersEntities db = new boardUsersEntities();
            return db.board_users.Where(x => x.id == id).FirstOrDefault().role;
        }
        
        /// <summary>
        /// Check to see if a username exists in the board users table
        /// </summary>
        /// <param name="username">The username to look up</param>
        /// <returns>bool</returns>
        public bool getExistingUser(string username)
        {
            if(db.board_users.Any(x => x.username == username))
            {
                return true;
            }
            return false;
        }

        // POST: BoardUsers/Edit/5
        /// <summary>
        /// Saves the changes of the board user to the db.
        /// </summary>
        /// <param name="board_users">The board_users object</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit([Bind(Include = "id,username,role")] board_users board_users)
        {
            //If an Administrator types into the url a SuperAdmins id, return not found
            if (board_users.role.Equals("SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }

            //Get the current values for the board user
            boardUsersEntities db = new boardUsersEntities();
            var currentRecordUsername = getRecordUsername(board_users.id);
            var currentRecordRole = getRecordRole(board_users.id);

            db.Entry(board_users).State = EntityState.Modified;

            ApplicationUser usr = UserManager.FindByName(currentRecordUsername);

            if (ModelState.IsValid)
            {
                //If the username is changed, update the identity accounts username column, update the email
                if (!currentRecordUsername.Equals(board_users.username))
                {
                    usr.UserName = board_users.username;
                    usr.Email = GetADEmail(board_users.username);
                    if (usr.Email.Equals(""))
                    {
                        ModelState.AddModelError(board_users.username, "Cannot find the email address from AD for the given username.");

                        //The Viewbag for the role drop down needs to be set by who is signed in
                        List<string> allRoles = getRoles();
                        ViewBag.userRoles = new SelectList(allRoles);

                        return View(board_users);
                    }
                    UserManager.Update(usr);
                }
                //If the role is changed, update the identity accounts role column
                if (!currentRecordRole.Equals(board_users.role))
                {
                    UserManager.RemoveFromRole(usr.Id, currentRecordRole);
                    UserManager.AddToRole(usr.Id, board_users.role);
                }

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(board_users);
        }

        // GET: BoardUsers/Delete/5
        /// <summary>
        /// Displays the Board Users Delete page allowing administrator to delete a board user from the db.
        /// </summary>
        /// <param name="id">The id of the board user to delete</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            board_users board_users = db.board_users.Find(id);
            if (board_users == null)
            {
                return HttpNotFound();
            }
            //If an Administrator types into the url a SuperAdmins id, return not found
            if (board_users.role.Equals("SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }
            return View(board_users);
        }

        // POST: BoardUsers/Delete/5
        /// <summary>
        /// Deletes the board user from the db. Removes the Identity account and assinged role.
        /// </summary>
        /// <param name="id">The id of the board user</param>
        /// <returns>View</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult DeleteConfirmed(Guid id)
        {
            board_users board_users = db.board_users.Find(id);
            ApplicationUser usr = UserManager.FindByName(board_users.username);

            //If an Administrator types into the url a SuperAdmins id, return not found
            if (board_users.role.Equals("SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }

            UserManager.RemoveFromRole(usr.Id, board_users.role);
            UserManager.Delete(usr);

            db.board_users.Remove(board_users);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }*/
    }
}
