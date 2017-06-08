﻿using DotNetThemeMVC.Models;
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
        /// Returns the administrators email address from AD
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
        /// Gets a list of PAL usernames that belong to an active directory group name
        /// </summary>
        /// <param name="adGroupName">The active directory group name</param>
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
                    //UserPrincipal user = p as UserPrincipal;
                    if (p != null)
                    {
                        accounts.Add(p.SamAccountName.ToLower());
                    }
                }
            }
            return accounts;
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
            //No Paremeter
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Administrators")]
        public ActionResult Edit(ADGroupRolesViewModel adGroupRolesViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    //Get a list of all the accounts that belong to the selected AD Group Name
                    List<string> accounts = GetADAccounts(adGroupRolesViewModel.groupName);
                    if (accounts == null)
                    {
                        ModelState.AddModelError(adGroupRolesViewModel.groupName, "Failed to retrieve users belonging to the AD Group.");
                        return View(adGroupRolesViewModel);
                    }

                    //Get the current list of roles for the active directory group
                    List<string> adGroupRoles = db.ad_group_roles.Where(x => x.group_name == adGroupRolesViewModel.groupName).Select(z => z.role_name).OrderBy(x => x).ToList();

                    //Loop through list of PAL names and create accounts(if needed) and add permissions(if selected)
                    foreach (string account in accounts)
                    {
                        //Check to see if an identity account exists
                        bool userExists = AccountExists(account);
                        if (userExists)
                        {
                            //If the permissions have changed we update the accounts
                            //SequenceEquals needs same order
                            adGroupRolesViewModel.groupRoles.OrderBy(x => x);
                            if(!adGroupRolesViewModel.groupRoles.SequenceEqual(adGroupRoles))
                            {
                                //shii...ooot
                                //authorizing multi groups
                                //usre can be multiple times
                                //however if oth groups say this user need permission of 'User'
                                //in the add thats fine it checks if the account has that permission already before assinging
                                //however in edit group role association removing a role from a group doesnt mean i can remove the role from the user
                                //i would have to see if they belong to other authorized groups and if only they don't have the same role assigned could i remove the user from the role
                            }
                        }
                    }
                    //db.Entry(ad_group_roles).State = EntityState.Modified;
                    //db.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(adGroupRolesViewModel);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Group Authorization.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to permission a group. We are aware of the issue and will investigate. Please try permissioning a group again. If the issue continues contact an Administrator.");
                return View(adGroupRolesViewModel);
            }
        }
        //some reference code
        /*
         using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DotNetThemeMVC.Models;
using System.DirectoryServices.AccountManagement;
using System.Web.Configuration;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

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

        // GET: ADGroupRoles
        public ActionResult Index()
        {
            return View(db.ad_group_roles.ToList());
        }

        // GET: ADGroupRoles/Create
        public ActionResult Create()
        {
            List<string> allRoles = getRoleNames();
            ViewBag.userRoles = new SelectList(allRoles);
            return View();
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

        public List<string> GetADAccounts(string adGroupName)
        {
            List<string> accounts = new List<string>();

            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, WebConfigurationManager.AppSettings["adAuthURL"].ToString());
            GroupPrincipal group = GroupPrincipal.FindByIdentity(ctx, adGroupName);

            if(group != null)
            {
                foreach(Principal p in group.GetMembers())
                {
                    //UserPrincipal user = p as UserPrincipal;
                    if(p != null)
                    {
                        accounts.Add(p.SamAccountName.ToLower());
                    }
                }
            }
            return accounts;
        }

        /// <summary>
        /// Returns the administrators email address from AD
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
        public void createAccount(string username)
        {
            string email = GetADEmail(username);
            if (!String.IsNullOrEmpty(email))
            {
                var user = new ApplicationUser { UserName = username, Email = GetADEmail(username), EmailConfirmed = true };
                UserManager.Create(user);
            }
        }

        // POST: ADGroupRoles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="SuperAdmin,Administrators")]
        public ActionResult Create([Bind(Include = "id,group_name,roles")] ad_group_roles ad_group_roles, List<string> selectedRoles)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    List<string> allRoles = getRoleNames();
                    ViewBag.userRoles = new SelectList(allRoles);

                    //Check to see if the AD Group Name already exists in the database
                    ad_group_roles existing = db.ad_group_roles.Where(x => x.group_name == ad_group_roles.group_name).FirstOrDefault();
                    if (existing != null)
                    {
                        ModelState.AddModelError(ad_group_roles.group_name, "AD Group already has Roles associated to it. Edit the record.");
                        return View(ad_group_roles);
                    }

                    //Get a list of all the accounts that belong to the selected AD Group Name
                    List<string> accounts = GetADAccounts(ad_group_roles.group_name);
                    if (accounts == null)
                    {
                        ModelState.AddModelError(ad_group_roles.group_name, "Failed to retrieve users belonging to the AD Group.");
                        return View(ad_group_roles);
                    }

                    foreach (string account in accounts)
                    {
                        bool userExists = accountExists(account);
                        if (!userExists)
                        {
                            createAccount(account);
                        }
                        if (selectedRoles != null && accountExists(account))
                        {
                            var userId = UserManager.FindByName(account).Id;

                            foreach (var role in selectedRoles)
                            {
                                if (!UserManager.IsInRole(userId, role))
                                {
                                    UserManager.AddToRole(userId, role);
                                }
                            }
                        }
                    }

                    foreach (var role in selectedRoles)
                    {//should add several rows to table
                        ad_group_roles ad = new ad_group_roles();
                        ad.group_name = ad_group_roles.group_name;
                        ad.role_name = role;
                        db.ad_group_roles.Add(ad);
                    }
                    //go through each role and add to ad group and save
                    //db.ad_group_roles.Add(ad_group_roles);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                return View(ad_group_roles);
            }
            catch (Exception ex)
            {
                Error error = new Error();
                error.handleError(ex, "Exception occured during Account Creation.");
                ModelState.AddModelError(string.Empty, "There was a problem when attempting to create an account. We are aware of the issue and will investigate. Please try creating an account again. If the issue continues contact an Administrator.");
                return View(ad_group_roles);
            }
        }

        // GET: ADGroupRoles/Edit/5
        public ActionResult Edit(string ADGroup)
        {
            if (ADGroup == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ad_group_roles ad_group_roles = db.ad_group_roles.Where(x => x.group_name == ADGroup).FirstOrDefault();
            if (ad_group_roles == null)
            {
                return HttpNotFound();
            }
            List<string> allRoles = getRoleNames();
            ViewBag.userRoles = new SelectList(allRoles);
            return View(db.ad_group_roles.Where(x => x.group_name == ADGroup).ToList());
        }

        // POST: ADGroupRoles/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,group_name,roles")] ad_group_roles ad_group_roles)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ad_group_roles).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(ad_group_roles);
        }

        // GET: ADGroupRoles/Delete/5
        public ActionResult Delete(string ADGroup)
        {
            if (ADGroup == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ad_group_roles ad_group_roles = db.ad_group_roles.Where(x => x.group_name == ADGroup).FirstOrDefault();
            if (ad_group_roles == null)
            {
                return HttpNotFound();
            }
            //Get this far at least one group -> role association exists, find the others if they exist
            return View(db.ad_group_roles.Where(x => x.group_name == ADGroup).ToList());
        }

        public List<String> GetADGroupRoles(string ADGroup)
        {
            List<string> allRoles = new List<string>();
            allRoles = db.ad_group_roles.Where(x => x.group_name == ADGroup).Select(z => z.role_name).ToList();
            return  allRoles;
        }

        // POST: ADGroupRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string ADGroup)
        {
            //Get a list of all the accounts that belong to the selected AD Group Name
            List<string> accounts = GetADAccounts(ADGroup);
            if (accounts == null)
            {
                ModelState.AddModelError("", "Failed to retrieve users belonging to the AD Group.");
                return View();
            }

            List<string> groupRoles = GetADGroupRoles(ADGroup);

            foreach (string account in accounts)
            {
                bool userExists = accountExists(account);
                if (userExists)
                {
                    if (groupRoles != null)
                    {
                        var userId = UserManager.FindByName(account).Id;

                        foreach (var role in groupRoles)
                        {
                            if (UserManager.IsInRole(userId, role))
                            {
                                UserManager.RemoveFromRole(userId, role);
                            }
                        }
                        if(UserManager.GetRoles(userId).Count() == 0)
                        {
                            ApplicationUser applicationUser = UserManager.FindById(userId);
                            UserManager.Delete(applicationUser);
                        }
                    }
                }
            }

            //not sure if this will delete all entries, test???
            foreach (var role in groupRoles)
            {
                ad_group_roles ad_group_roles = db.ad_group_roles.Where(x => x.role_name == role).FirstOrDefault();
                db.ad_group_roles.Remove(ad_group_roles);
            }
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
        }
    }
}
         */

    }
}