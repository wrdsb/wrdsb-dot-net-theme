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

namespace WebApplication1.Controllers
{
    public class RoleController : Controller
    {/*
        private ApplicationDbContext db = new ApplicationDbContext();

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
                boardUsersEntities db = new boardUsersEntities();

                string currentRoleName = roleManager.FindById(roleViewModels.id).Name;
                List<board_users> board_users = db.board_users.Where(x => x.role == currentRoleName).ToList();

                IdentityRole role = roleManager.FindById(roleViewModels.id);
                role.Name = roleViewModels.Name;

                roleManager.Update(role);

                //Update the board_users table with the new name
                foreach (var board_user in board_users)
                {
                    board_user.role = role.Name;
                }
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

            boardUsersEntities db = new boardUsersEntities();
            List<board_users> board_users = db.board_users.ToList();

            bool roleIsAssigned = false;
            int roleCount = 0;
            foreach (var board_user in board_users)
            {
                if (board_user.role.Equals(role.Name))
                {
                    roleIsAssigned = true;
                    roleCount += 1;
                }
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
                ModelState.AddModelError("", "Cannot remove a Role that " + roleCount + " users are assigned to.");
                return View(roleViewModels);
            }
            else
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new ApplicationDbContext()));
                //If I try to remove using 'role' defined earlier an error happens on roleManager.Delete:
                //The object cannot be deleted because it was not found in the ObjectStateManager.
                //This work around works
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
        }*/
    }
}
