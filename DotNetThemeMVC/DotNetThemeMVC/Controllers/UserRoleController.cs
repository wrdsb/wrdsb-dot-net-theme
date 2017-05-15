using DotNetThemeMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using System.Collections.ObjectModel;
using Microsoft.AspNet.Identity.EntityFramework;
using PagedList;
using System.Net;
using System.DirectoryServices.AccountManagement;
using System.Web.Services;

namespace DotNetThemeMVC.Controllers
{
    public class UserRoleController : Controller
    {
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

        // GET: UserRole/Index
        /// <summary>
        /// Displays the UserRole Index page listing all board users. Allows for searching or filtering the users.
        /// </summary>
        /// <param name="userViewModel">The User model</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Index(UserViewModel userViewModel)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));

            //Get all board users
            var users = UserManager.Users.Where(x => !x.UserName.Contains("@")).ToList();

            //If the signed in users role is Administrators, filter out users whose role is SuperAdmin
            if (isAdministrator())
            {
                var superUsers = roleManager.Roles.Single(x => x.Name == "SuperAdmin").Users;
                foreach (var superUser in superUsers)
                {
                    users.RemoveAll(x => x.Id == superUser.UserId);
                }
            }

            //If there is a role filter,filter down the list unless it's All Roles option
            if (!String.IsNullOrEmpty(userViewModel.roleFilter) && !userViewModel.roleFilter.Equals("All Roles"))
            {
                var roleUsers = roleManager.Roles.Single(x => x.Name.Equals(userViewModel.roleFilter)).Users;
                users = (from r in roleUsers join u in users on r.UserId equals u.Id select u).Distinct().ToList();
            }

            //If there is a search term, filter down the list
            if (!String.IsNullOrEmpty(userViewModel.searchString))
            {
                users = users.Where(x => x.UserName.Contains(userViewModel.searchString)).ToList();
            }

            //Sort the list
            switch (userViewModel.sortOrder)
            {
                case "name_desc":
                    users = users.OrderByDescending(x => x.UserName).ToList();
                    break;
                default:
                    users = users.OrderBy(x => x.UserName).ToList();
                    break;
            }

            var model = new Collection<UserRoleViewModel>();

            //All filtering has been completed on the user list.
            //Get the Roles for every user, pass it into the Model and then to the ViewModel.
            foreach (var user in users)
            {
                var userRoles = UserManager.GetRoles(user.Id);
                model.Add(new UserRoleViewModel { User = user, userRoles = userRoles });
            }

            //Set the default page
            if (userViewModel.page == 0)
            {
                userViewModel.page = 1;
            }
            //Set the default page size
            if (userViewModel.pageSize == 0)
            {
                userViewModel.pageSize = 10;
            }

            //Change it to a MVC Paged List
            userViewModel.Users = model.ToPagedList(userViewModel.page, userViewModel.pageSize);

            //Get the options for the Roles Filter drop down and the Page Size drop down
            ViewBag.PageSize = getPageOptions();
            ViewBag.UserRoles = getRoleFilterOptions();

            return View(userViewModel);
        }

        // GET: UserRole/Create
        /// <summary>
        /// Displays the UserRole Add page allowing administrator to add a board user to the db.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Create()
        {
            UserRoleViewModel model = new UserRoleViewModel();
            model.allRoles = getRoleNames();
            model.userRoles = new List<string> { };
            return View(model);
        }

        // POST: UserRole/Create
        /// <summary>
        /// Adds a board user to the db. Creates the Identity account and grants the selected role.
        /// </summary>
        /// <param name="model">The User model</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Create(UserRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (UserManager.FindByName(model.User.UserName) == null)
                {
                    var email = getADEmail(model.User.UserName);

                    if (String.IsNullOrEmpty(email))
                    {
                        ModelState.AddModelError(model.User.UserName, "Failed to retrieve email for username from Active Directory");
                        model.allRoles = getRoleNames();
                        return View(model);
                    }
                    var user = new ApplicationUser { UserName = model.User.UserName, Email = email, EmailConfirmed = true };
                    var result = UserManager.Create(user);

                    //Assign the Roles
                    if (model.userRoles != null)
                    {
                        foreach (var role in model.userRoles)
                        {
                            UserManager.AddToRole(user.Id, role);
                        }
                    }
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(model.User.UserName, "This user exists.");
                    model.allRoles = getRoleNames();
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: UserRole/Edit/5
        /// <summary>
        /// Displays the Userrole Edit page allowing the administrator to edit an existing board users information.
        /// </summary>
        /// <param name="id">The id of the board user to edit</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            ApplicationUser user = UserManager.Users.Where(x => x.Id == id).First();
            if (user == null)
            {
                return HttpNotFound();
            }
            UserRoleViewModel model = new UserRoleViewModel();

            //Get a List of Roles Names for the given user id
            model.userRoles = UserManager.GetRoles(id);

            //Get the entire list of Role Names
            model.allRoles = getRoleNames();

            var userRoles = UserManager.GetRoles(id);
            model.User = user;

            if (UserManager.IsInRole(id, "SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: UserRole/Edit/5
        /// <summary>
        /// Saves the changes of the board user to the db.
        /// </summary>
        /// <param name="model">The User model</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit(UserRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = UserManager.Users.Where(x => x.Id == model.User.Id).First();
                if (user.UserName != model.User.UserName)
                {
                    user.Email = getADEmail(model.User.UserName);
                    if (user.Email.Equals(""))
                    {
                        ModelState.AddModelError(model.User.UserName, "Cannot find the email address from AD for the given username.");

                        List<SelectListItem> allRoles = getRoles();
                        ViewBag.userRoles = allRoles;

                        return View(model);
                    }
                    user.UserName = model.User.UserName;
                    UserManager.Update(user);
                }

                //Update the Role Assignments
                var roles = UserManager.GetRoles(model.User.Id);
                UserManager.RemoveFromRoles(model.User.Id, roles.ToArray());
                foreach (var userRole in model.userRoles)
                {
                    UserManager.AddToRole(model.User.Id, userRole);
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: UserRole/Delete/5
        /// <summary>
        /// Displays the Board Users Delete page allowing administrator to delete a board user from the db.
        /// </summary>
        /// <param name="id">The id of the board user to delete</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            ApplicationUser user = UserManager.Users.Where(x => x.Id == id).First();
            if (user == null)
            {
                return HttpNotFound();
            }
            UserRoleViewModel model = new UserRoleViewModel();

            //Get a List of Roles Names for the given user id
            model.userRoles = UserManager.GetRoles(id);
            model.User = user;
            if (UserManager.IsInRole(id, "SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: UserRole/Delete/5
        /// <summary>
        /// Deletes the board user from the db. Removes the Identity account and assinged role.
        /// </summary>
        /// <param name="id">The id of the board user</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Delete(UserRoleViewModel model)
        {
            ApplicationUser usr = UserManager.FindById(model.User.Id);

            //If an Administrator types into the url a SuperAdmins id, return not found
            if (UserManager.IsInRole(model.User.Id, "SuperAdmin") && isAdministrator())
            {
                return HttpNotFound();
            }
            //Update the Role Assignments
            var roles = UserManager.GetRoles(model.User.Id);
            UserManager.RemoveFromRoles(model.User.Id, roles.ToArray());

            UserManager.Delete(usr);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Returns the users email address from AD
        /// </summary>
        /// <param name="username">The input value for username</param>
        /// <returns>string</returns>
        public string getADEmail(string username)
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
            if (UserManager.IsInRole(userId, "Administrators") && !UserManager.IsInRole(userId, "SuperAdmin"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Generates the options in the page size drop down
        /// </summary>
        /// <returns>List<SelectListItem></returns>
        public List<SelectListItem> getPageOptions()
        {
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem { Text = "10", Value = "10" });
            items.Add(new SelectListItem { Text = "25", Value = "25" });
            items.Add(new SelectListItem { Text = "50", Value = "50" });
            items.Add(new SelectListItem { Text = "100", Value = "100" });

            return items;
        }

        /// <summary>
        /// Gets the list of roles based on admin or superadmin status
        /// </summary>
        /// <returns>List<SelectListItem></returns>
        public List<SelectListItem> getRoles()
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            List<SelectListItem> roles = roleManager.Roles.Select(x => new SelectListItem() { Value = x.Id, Text = x.Name }).ToList();

            if (isAdministrator())
            {
                roles.Remove(roles.Single(x => x.Value == "SuperAdmin"));
                return roles;
            }
            else
            {
                return roles;
            }
        }

        /// <summary>
        /// Gets the list of roles based on admin or superadmin status
        /// </summary>
        /// <returns>List<string></returns>
        public List<string> getRoleNames()
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

        /// <summary>
        /// Gets the list of roles based on admin or superadmin status
        /// </summary>
        /// <returns>List<SelectListItem></returns>
        public List<SelectListItem> getRoleFilterOptions()
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            List<SelectListItem> roles = roleManager.Roles.Select(x => new SelectListItem() { Value = x.Name, Text = x.Name }).ToList();

            //Add an item that resets the role filter if selected
            roles.Insert(0, new SelectListItem { Value = "All Roles", Text = "All Roles" });

            if (isAdministrator())
            {
                roles.Remove(roles.Single(x => x.Value == "SuperAdmin"));
                return roles;
            }
            else
            {
                return roles;
            }
        }

        ///For future release
        /// <summary>
        /// Autocomplete for AD Group Searches
        /// </summary>
        /// <param name="search">The search term</param>
        /// <returns>Json</returns>
        [HttpPost]
        public ActionResult getADGroups(string search)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, System.Web.Configuration.WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            GroupPrincipal groupPrincipal = new GroupPrincipal(context);

            groupPrincipal.SamAccountName = search + "*";

            PrincipalSearcher principalSearch = new PrincipalSearcher(groupPrincipal);

            List<string> results = new List<string>();
            var result = new List<KeyValuePair<string, string>>();

            int count = 0;
            var list = new List<JsonResults>();

            foreach (var found in principalSearch.FindAll())
            {
                if (count == 5)
                {
                    break;
                }
                list.Add(new JsonResults { id = count.ToString(), label = found.SamAccountName, value = found.SamAccountName });
                count += 1;
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }
    }

    /// <summary>
    /// Json class
    /// </summary>
    public class JsonResults
    {
        public string id { get; set; }
        public string label { get; set; }
        public string value { get; set; }
    }
}