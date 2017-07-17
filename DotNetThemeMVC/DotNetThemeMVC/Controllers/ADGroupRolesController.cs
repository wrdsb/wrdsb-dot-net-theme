using DotNetThemeMVC.Models;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections.ObjectModel;
using System.DirectoryServices.AccountManagement;
using System.Web.Configuration;
using System.Net;
using System.Data.Entity;

namespace DotNetThemeMVC.Controllers
{
    public class ADGroupRolesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
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
        /// Gets the list of roles based on admin or superadmin status
        /// </summary>
        /// <returns>List<string></returns>
        public List<string> GetRoleNames()
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
        /// Checks to see if the currently signed in user is in the Administrator Role
        /// </summary>
        /// <returns>boolean</returns>
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
        /// Checks to see if an Account Exists for the supplied username
        /// </summary>
        /// <param name="username">The input value for username</param>
        /// <returns>boolean</returns>
        public bool AccountExists(string username)
        {
            if (UserManager.FindByName(username) != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the users email address from Active Directory
        /// </summary>
        /// <param name="username">The input value for username</param>
        /// <returns>string</returns>
        public string GetADEmail(string username)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            UserPrincipal user = UserPrincipal.FindByIdentity(context, username);
            return user.EmailAddress;
        }

        /// <summary>
        /// Creates an account for the supplied username
        /// </summary>
        /// <param name="username">The input value for username</param>
        public void CreateAccount(string username)
        {
            string email = GetADEmail(username);
            if (!String.IsNullOrEmpty(email))
            {
                var user = new ApplicationUser { UserName = username, Email = GetADEmail(username), EmailConfirmed = true };
                UserManager.Create(user);
            }
        }

        /// <summary>
        /// Gets a list of PAL usernames that belong to an Active Directory group name
        /// </summary>
        /// <param name="adGroupName">The input value for active directory group name</param>
        /// <returns>List<string></returns>
        public List<string> GetADAccounts(string adGroupName)
        {
            List<string> accounts = new List<string>();

            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            GroupPrincipal group = GroupPrincipal.FindByIdentity(ctx, adGroupName);

            if (group != null)
            {
                foreach (Principal p in group.GetMembers())
                {
                    //Make sure p is a user and not a group within a group
                    if (p != null && p is UserPrincipal)
                    {
                        //Filter out non character names as well
                        if (p.SamAccountName.All(Char.IsLetter))
                        {
                            accounts.Add(p.SamAccountName.ToLower());
                        }
                    }
                }
            }
            return accounts;
        }

        /// <summary>
        /// Checks to see if a username is a member of a Active Directory gruop
        /// </summary>
        /// <param name="account">The input value for username</param>
        /// <param name="adGroup">The input value for active directory group name</param>
        /// <returns>boolean</returns>
        public bool IsMemberOfADGroup(string account, string adGroup)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain, WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            UserPrincipal user = UserPrincipal.FindByIdentity(context, account);
            GroupPrincipal group = GroupPrincipal.FindByIdentity(context, adGroup);

            if (user.IsMemberOf(group))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks to see if a pending role to be removed for a user is granted from the Active Directory Group
        /// </summary>
        /// <param name="account">The input value for username</param>
        /// <param name="removedRole">The input value for the removed role</param>
        /// <param name="groupName">The input value for the active directory group name</param>
        /// <returns>boolean</returns>
        public bool IsPermissionedByGroupToEdit(string account, string removedRole, string groupName)
        {
            //List<string> adGroupNames = db.ad_group_roles.Select(x => x.group_name).Distinct().ToList();
            //foreach (var adGroupName in adGroupNames)
            //{
                if (IsMemberOfADGroup(account, groupName))
                {
                    List<string> roles = db.ad_group_roles.Where(x => x.group_name == groupName).Select(x => x.role_name).ToList();
                    foreach (var role in roles)
                    {
                        if (role.Equals(removedRole))
                        {
                            return true;
                        }
                    }
                }
            //}
            return false;
        }

        /// <summary>
        /// Checks to see if a pending role to be removed for a user is granted from any Active Directory Group
        /// </summary>
        /// <param name="account">The input value for username</param>
        /// <param name="removedRole">The input value for the removed role</param>
        /// <param name="excludeGroup">The input value for the active directory group name</param>
        /// <returns>boolean</returns>
        public bool IsPermissionedByOtherGroups(string account, string removedRole, string excludeGroup)
        {
            List<string> adGroupNames = db.ad_group_roles.Select(x => x.group_name).Distinct().ToList();
            adGroupNames.Remove(excludeGroup);
            foreach (var adGroupName in adGroupNames)
            {
                if (IsMemberOfADGroup(account, adGroupName))
                {
                    List<string> roles = db.ad_group_roles.Where(x => x.group_name == adGroupName).Select(x => x.role_name).ToList();
                    foreach (var role in roles)
                    {
                        if (role.Equals(removedRole))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // GET: ADGroupRoles
        /// <summary>
        /// Displays the AD Group Roles Index page listing all AD Group to Role associations.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Index()
        {
            List<ADGroupRolesViewModel> model = new List<ADGroupRolesViewModel>();

            List<string> adGroupNames = db.ad_group_roles.Select(x => x.group_name).Distinct().ToList();
            foreach (var adGroupName in adGroupNames)
            {
                List<string> adGroupRoles = db.ad_group_roles.Where(x => x.group_name == adGroupName).Select(z => z.role_name).ToList();
                ADGroupRolesViewModel adGroupRole = new ADGroupRolesViewModel();
                adGroupRole.groupName = adGroupName;
                adGroupRole.groupRoles = adGroupRoles;
                model.Add(adGroupRole);
            }
            return View(model);
        }

        // GET: ADGroupRoles/Create
        /// <summary>
        /// Displays the AD Group Role Add page allowing administrators to authorize an AD Group to the application.
        /// Roles are optional.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Create()
        {
            List<string> allRoles = GetRoleNames();
            ADGroupRolesViewModel model = new ADGroupRolesViewModel();
            model.allRoles = allRoles;
            return View(model);
        }
        //POST: ADGroupRoles/Create
        /// <summary>
        /// Authorizes an Active Directory Group to the Application.
        /// Creates identity accounts for members of Active Directory Group if needed.
        /// Permissions the accounts if selected.
        /// </summary>
        /// <param name="adGroupRolesViewModel">The ad group role model</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Create(ADGroupRolesViewModel adGroupRolesViewModel)
        {
            try
            {
                adGroupRolesViewModel.allRoles = GetRoleNames();
                if (ModelState.IsValid)
                {
                    //Check to see if the AD Group Name already exists in the database
                    ad_group_roles existing = db.ad_group_roles.Where(x => x.group_name == adGroupRolesViewModel.groupName).FirstOrDefault();
                    if (existing != null)
                    {
                        ModelState.AddModelError(adGroupRolesViewModel.groupName, "Active Directory Group is already authorized.");
                        return View(adGroupRolesViewModel);
                    }

                    //Get a list of all the accounts that belong to the AD Group Name
                    List<string> accounts = GetADAccounts(adGroupRolesViewModel.groupName);
                    if (accounts == null)
                    {
                        ModelState.AddModelError(adGroupRolesViewModel.groupName, "Failed to retrieve users belonging to the AD Group.");
                        return View(adGroupRolesViewModel);
                    }

                    //Loop through list of PAL names and create accounts(if needed) and add permissions(if selected)
                    foreach (string account in accounts)
                    {
                        //Check to see if an identity account exists
                        bool userExists = AccountExists(account);
                        if (!userExists)
                        {
                            //Create an identity account for the supplied PAL username
                            CreateAccount(account);
                        }
                        //Check if account exists before attempting to apply permissions
                        //CreateAccount function doesn't create an account if no associated email can be found
                        //Check if any roles were selected, can authorize a group without setting roles
                        if (adGroupRolesViewModel.groupRoles != null && AccountExists(account))
                        {
                            //Get the user id of the account
                            var userId = UserManager.FindByName(account).Id;

                            //For all the selected roles, assign the role to the user
                            foreach (var role in adGroupRolesViewModel.groupRoles)
                            {
                                if (!UserManager.IsInRole(userId, role))
                                {
                                    UserManager.AddToRole(userId, role);
                                }
                            }
                        }
                    }

                    //Check if any roles were selected 
                    if (adGroupRolesViewModel.groupRoles != null)
                    {
                        //For all the selected roles, save a record to the ad_group_roles table
                        foreach (var role in adGroupRolesViewModel.groupRoles)
                        {
                            ad_group_roles ad = new ad_group_roles();
                            ad.group_name = adGroupRolesViewModel.groupName;
                            ad.role_name = role;
                            db.ad_group_roles.Add(ad);
                        }
                        db.SaveChanges();
                    }
                    return RedirectToAction("Index");
                }
                return View(adGroupRolesViewModel);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Group Authorization.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to authorize a group. We are aware of the issue and will investigate. Please try authorizing a group again. If the issue continues contact an Administrator.");

                List<string> allRoles = GetRoleNames();
                adGroupRolesViewModel.allRoles = allRoles;
                return View(adGroupRolesViewModel);
            }
        }

        // GET: ADGroupRoles/Edit/5
        /// <summary>
        /// Displays the Edit page allowing an administrator to edit the roles associated with an Active Directory Group.
        /// </summary>
        /// <param name="id">The Active Directory Group to edit</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit(string id)
        {
            //No Parameter
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Unable to find group based on parameter
            ad_group_roles ad_group_roles = db.ad_group_roles.Where(x => x.group_name == id).FirstOrDefault();
            if (ad_group_roles == null)
            {
                return HttpNotFound();
            }

            //Get the Active Directory Group Roles and save it to the model
            List<string> allRoles = GetRoleNames();
            List<string> adGroupRoles = db.ad_group_roles.Where(x => x.group_name == id).Select(z => z.role_name).ToList();

            ADGroupRolesViewModel model = new ADGroupRolesViewModel();
            model.allRoles = allRoles;
            model.groupName = id;
            model.groupRoles = adGroupRoles;
            return View(model);
        }

        //POST: ADGroupRoles/Edit
        /// <summary>
        /// Updates the roles associated with an Active Directory Group and all the associated user accounts.
        /// </summary>
        /// <param name="adGroupRolesViewModel">The Active Directory Group Model to edit</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit(ADGroupRolesViewModel adGroupRolesViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //Check to see that permissions were changed for the active directory group

                    //Get the current list of roles for the active directory group
                    List<string> adGroupRoles = db.ad_group_roles.Where(x => x.group_name == adGroupRolesViewModel.groupName).Select(z => z.role_name).OrderBy(x => x).ToList();

                    //Sequence requires same order
                    adGroupRolesViewModel.groupRoles.OrderBy(x => x);

                    //If the selected Roles are different to whats on record we update account permissions
                    if (!adGroupRolesViewModel.groupRoles.SequenceEqual(adGroupRoles))
                    {
                        //Get a list of all the accounts that belong to the selected AD Group Name
                        List<string> accounts = GetADAccounts(adGroupRolesViewModel.groupName);
                        if (accounts == null)
                        {
                            ModelState.AddModelError(adGroupRolesViewModel.groupName, "Failed to retrieve users belonging to the AD Group.");
                            return View(adGroupRolesViewModel);
                        }

                        //Compare newly selected roles and current and get a list of removed roles
                        List<string> removedRoles = adGroupRoles.Except(adGroupRolesViewModel.groupRoles).ToList();
                        //Compare current and newly selected roles and get a list of newly selected roles
                        List<string> newRoles = adGroupRolesViewModel.groupRoles.Except(adGroupRoles).ToList();

                        //Loop through list of PAL names and create accounts(if needed) and add permissions(if selected)
                        foreach (string account in accounts)
                        {
                            //Check to see if an identity account exists
                            bool userExists = AccountExists(account);
                            if (userExists)
                            {
                                if(removedRoles.Count > 0)
                                {
                                    foreach(var role in removedRoles)
                                    {
                                        //Does the removed role come from the group that is being edited
                                        if (IsPermissionedByGroupToEdit(account, role, adGroupRolesViewModel.groupName))
                                        {
                                            //Does the removed role come from other groups that still apply the role
                                            if (!IsPermissionedByOtherGroups(account, role, adGroupRolesViewModel.groupName))
                                            {
                                                var userId = UserManager.FindByName(account).Id;

                                                if (UserManager.IsInRole(userId, role))
                                                {
                                                    UserManager.RemoveFromRole(userId, role);
                                                }
                                            }
                                        }
                                    }
                                }

                                //For every new role assign the user the role if they aren't assigned it yet
                                if (newRoles.Count > 0)
                                {
                                    foreach (var role in newRoles)
                                    {
                                        var userId = UserManager.FindByName(account).Id;

                                        if (!UserManager.IsInRole(userId, role))
                                        {
                                            UserManager.AddToRole(userId, role);
                                        }
                                    }
                                }
                            }
                        }

                        //For all the removed roles, remove the record to the ad_group_roles table
                        foreach (var role in removedRoles)
                        {
                            ad_group_roles searchGroup = db.ad_group_roles.Where(x => x.group_name == adGroupRolesViewModel.groupName).Where(x => x.role_name == role).FirstOrDefault();

                            db.ad_group_roles.Attach(searchGroup);
                            db.Entry(searchGroup).State = EntityState.Deleted;
                        }
                        //For all the new roles, add a record to the ad_group_roles table
                        foreach (var role in newRoles)
                        {
                            ad_group_roles ad = new ad_group_roles();
                            ad.group_name = adGroupRolesViewModel.groupName;
                            ad.role_name = role;
                            db.ad_group_roles.Add(ad);
                        }
                        db.SaveChanges();

                        return RedirectToAction("Index");
                    }
                }
                return View(adGroupRolesViewModel);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Group Authorization.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to permission a group. We are aware of the issue and will investigate. Please try permissioning a group again. If the issue continues contact an Administrator.");
                List<string> allRoles = GetRoleNames();
                adGroupRolesViewModel.allRoles = allRoles;
                return View(adGroupRolesViewModel);
            }
        }

        //GET: ADGroupRoles/Delete
        /// <summary>
        /// Displays the Delete page allowing an administrator to delete an Active Directory Group and Roles.
        /// </summary>
        /// <param name="id">The input value for the Active Directory Group</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Unable to find group based on parameter
            ad_group_roles ad_group_roles = db.ad_group_roles.Where(x => x.group_name == id).FirstOrDefault();
            if (ad_group_roles == null)
            {
                return HttpNotFound();
            }

            //Get the Active Directory Group Roles and save it to the model
            List<string> allRoles = GetRoleNames();
            List<string> adGroupRoles = db.ad_group_roles.Where(x => x.group_name == id).Select(z => z.role_name).ToList();

            ADGroupRolesViewModel model = new ADGroupRolesViewModel();
            model.allRoles = allRoles;
            model.groupName = id;
            model.groupRoles = adGroupRoles;
            return View(model);
        }

        //POST: ADGroupRoles/Delete
        /// <summary>
        /// Deletes the Active Directory Group and Roles, and all the associated user accounts if they have no roles.
        /// </summary>
        /// <param name="id">The input value for the Active Directory Group</param>
        /// <returns>View</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult DeleteConfirmed(string id)
        {
            try
            {
                ADGroupRolesViewModel adGroupRolesViewModel = new ADGroupRolesViewModel();
                adGroupRolesViewModel.groupName = id;
                adGroupRolesViewModel.groupRoles = db.ad_group_roles.Where(x => x.group_name == id).Select(z => z.role_name).ToList();
                //Get a list of all the accounts that belong to the selected AD Group Name
                List<string> accounts = GetADAccounts(adGroupRolesViewModel.groupName);
                if (accounts == null)
                {
                    ModelState.AddModelError(id, "Failed to retrieve users belonging to the AD Group.");
                    return View(id);
                }

                //Loop through list of PAL names and create accounts(if needed) and add permissions(if selected)
                foreach (string account in accounts)
                {
                    //Check to see if an identity account exists
                    bool userExists = AccountExists(account);
                    if (userExists)
                    {
                        if (adGroupRolesViewModel.groupRoles.Count > 0)
                        {
                            foreach (var role in adGroupRolesViewModel.groupRoles)
                            {
                                //Does the role come from the group that is being deleted
                                if (IsPermissionedByGroupToEdit(account, role, adGroupRolesViewModel.groupName))
                                {
                                    //Does the role come from other groups that still apply the role
                                    if (!IsPermissionedByOtherGroups(account, role, adGroupRolesViewModel.groupName))
                                    {
                                        var userId = UserManager.FindByName(account).Id;

                                        if (UserManager.IsInRole(userId, role))
                                        {
                                            UserManager.RemoveFromRole(userId, role);
                                        }
                                    }
                                }
                            }
                            //If the account has no roles, delete it
                            var user = UserManager.FindByName(account);
                            var roles = UserManager.GetRoles(user.Id);
                            if (roles.Count == 0)
                            {
                                UserManager.Delete(user);
                            }
                        }
                    }
                }
                foreach (var role in adGroupRolesViewModel.groupRoles)
                {
                    ad_group_roles searchGroup = db.ad_group_roles.Where(x => x.group_name == adGroupRolesViewModel.groupName).Where(x => x.role_name == role).FirstOrDefault();

                    db.ad_group_roles.Attach(searchGroup);
                    db.Entry(searchGroup).State = EntityState.Deleted;
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Group Authorization.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to delete a group. We are aware of the issue and will investigate. Please try permissioning a group again. If the issue continues contact an Administrator.");

                return View(id);
            }
        }

        /// <summary>
        /// Json class for the Active Directory name look up function
        /// </summary>
        public class JsonResults
        {
            public string id { get; set; }
            public string label { get; set; }
            public string value { get; set; }
        }

        /// <summary>
        /// Searches Active Directory group names and returns top 5 results
        /// </summary>
        /// <param name="search">The input value for searching Active Directory Group names</param>
        /// <returns>Json</returns>
        [HttpPost]
        public ActionResult getADGroups(string search)
        {
            PrincipalContext context = new PrincipalContext(ContextType.Domain,System.Web.Configuration.WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            GroupPrincipal groupPrincipal = new GroupPrincipal(context);

            groupPrincipal.Name = search + "*";

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
                list.Add(new JsonResults { id = count.ToString(), label = found.Name, value = found.Name });
                count += 1;
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }
    }
}