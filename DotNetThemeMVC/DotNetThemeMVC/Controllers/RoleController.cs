using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
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
using DotNetThemeMVC;

namespace WebApplication1.Controllers
{
    public class RoleController : Controller
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
        /// Returns an IdentityRole object based on an id.
        /// </summary>
        /// <param name="id">The id of an IdentityRole</param>
        /// <returns>IdentityRole</returns>
        public IdentityRole getIdentityRole(string id)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            IdentityRole role = roleManager.FindById(id);
            return role;
        }

        // GET: Role
        /// <summary>
        /// Displays the Roles Index page listing all roles.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Index()
        {
            var identityRoles = db.Roles.ToList();
            List<RoleViewModels> allRoles = new List<RoleViewModels>();
            foreach (var identityrole in identityRoles)
            {
                RoleViewModels role = new RoleViewModels();
                role.id = identityrole.Id;
                role.Name = identityrole.Name;
                allRoles.Add(role);
            }
            return View(allRoles);
        }

        // GET: Role/Create
        /// <summary>
        /// Displays the Role Add page allowing superadmin to add a role to the db.
        /// </summary>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Role/Create
        /// <summary>
        /// Adds a role to the db.
        /// </summary>
        /// <param name="roleViewModels">The Role object to add</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Create([Bind(Include = "id,Name")] RoleViewModels roleViewModels)
        {
            if (ModelState.IsValid)
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
                if (roleManager.RoleExists(roleViewModels.Name) == false)
                {
                    var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                    role.Name = roleViewModels.Name;
                    roleManager.Create(role);

                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(roleViewModels.Name, "Role exists.");
                    return View(roleViewModels);
                }
            }

            return View(roleViewModels);
        }

        // GET: Role/Edit/5
        /// <summary>
        /// Displays the Role Edit page allowing the superadmin to edit existing roles.
        /// </summary>
        /// <param name="id">The id of the Role to edit</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Get the Identity Role
            IdentityRole role = getIdentityRole(id);

            //Assign the Identity Roles
            RoleViewModels roleViewModels = new RoleViewModels();
            roleViewModels.id = role.Id;
            roleViewModels.Name = role.Name;

            if (roleViewModels == null)
            {
                return HttpNotFound();
            }
            return View(roleViewModels);
        }

        // POST: Role/Edit/5
        /// <summary>
        /// Saves the changes of the role to the db.
        /// </summary>
        /// <param name="roleViewModels">The role object</param>
        /// <returns>View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Edit([Bind(Include = "id,Name")] RoleViewModels roleViewModels)
        {
            if (ModelState.IsValid)
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));

                IdentityRole role = roleManager.FindById(roleViewModels.id);
                role.Name = roleViewModels.Name;
                roleManager.Update(role);

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(roleViewModels);
        }

        // GET: Role/Delete/5
        /// <summary>
        /// Displays the Role Delete page allowing the superadmin to delete a role from the db.
        /// </summary>
        /// <param name="id">The id of the role to delete</param>
        /// <returns>View</returns>
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //Get the Identity Role
            IdentityRole role = getIdentityRole(id);

            //Assign the Identity Roles
            RoleViewModels roleViewModels = new RoleViewModels();
            roleViewModels.id = role.Id;
            roleViewModels.Name = role.Name;

            if (roleViewModels == null)
            {
                return HttpNotFound();
            }

            return View(roleViewModels);
        }

        // POST: Role/Delete/5
        /// <summary>
        /// Deletes the role from the db.
        /// </summary>
        /// <param name="id">The id of the role</param>
        /// <returns>View</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            IdentityRole role = getIdentityRole(id);

            var users = UserManager.Users.Where(x => !x.UserName.Contains("@")).ToList();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
            var roleUsers = roleManager.Roles.Single(x => x.Name.Equals(role.Name)).Users;
            users = (from r in roleUsers join u in users on r.UserId equals u.Id select u).Distinct().ToList();

            bool roleIsAssigned = false;
            int roleCount = users.Count;

            if(roleCount > 0)
            {
                roleIsAssigned = true;
            }

            //Assign the Identity Roles
            RoleViewModels roleViewModels = new RoleViewModels();
            roleViewModels.id = role.Id;
            roleViewModels.Name = role.Name;

            if (roleViewModels == null)
            {
                return HttpNotFound();
            }

            if (roleIsAssigned)
            {
                ModelState.AddModelError(roleViewModels.Name, "Cannot remove a Role that " + roleCount + " users are assigned to.");
                return View(roleViewModels);
            }
            else
            {
                var roleToDelete = roleManager.FindByName(role.Name);
                roleManager.Delete(roleToDelete);

                return RedirectToAction("Index");
            }
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
