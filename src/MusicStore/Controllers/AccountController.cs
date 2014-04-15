using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using MusicStore.Models;
using System.Security.Principal;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    //[Authorize]
    public class AccountController : Controller
    {
        public AccountController()
            //Bug: Using an in memory store
            //: this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
            //: this(new UserManager<ApplicationUser>(new InMemoryUserStore<ApplicationUser>()))
        {
        }

        //public AccountController(UserManager<ApplicationUser> userManager)
        //{
        //    UserManager = userManager;
        //}

        /// <summary>
        /// TODO: Temporary ugly work around (making this static) to enable creating a static InMemory UserManager. Will go away shortly.
        /// </summary>
        public static UserManager<ApplicationUser> UserManager { get; set; }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid == true)
            {
                var user = await UserManager.FindByUserNamePasswordAsync(model.UserName, model.Password);
                if (user != null)
                {
                    await SignIn(user, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        [HttpGet] //TODO: Do we need this. Without this I seem to be landing here irrespective of the HTTP verb?
        public IActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //Bug: https://github.com/aspnet/DataAnnotations/issues/21
            //if (ModelState.IsValid == true)
            {
                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignIn(user, isPersistent: false);
                    //Bug: No helper methods
                    //return RedirectToAction("Index", "Home");
                    return Redirect("/");
                }
                else
                {
                    //https://github.com/aspnet/Identity/issues/37
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Manage
        public async Task<IActionResult> Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = await HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = await HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid == true)
                {
                    var user = new ApplicationUser() { UserName = this.Context.User.Identity.GetUserId() };
                    IdentityResult result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        //Bug: No helper method
                        //return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                        return View();
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = null;
                ModelState.TryGetValue("OldPassword", out state);

                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid == true)
                {
                    var user = new ApplicationUser() { UserName = this.Context.User.Identity.GetUserId() };
                    IdentityResult result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        //Bug: No helper method
                        //return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult LogOff()
        {
            this.Context.Response.SignOut();
            //Bug: No helper
            //return RedirectToAction("Index", "Home");
            return Redirect("/");
        }

        //Bug: Controllers need to be disposable? 
        protected void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }

            //base.Dispose(disposing);
        }

        #region Helpers

        private async Task SignIn(ApplicationUser user, bool isPersistent)
        {
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            this.Context.Response.SignIn(identity, new AuthenticationProperties() { IsPersistent = isPersistent });
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private async Task<bool> HasPassword()
        {
            var user = await UserManager.FindByNameAsync(this.Context.User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            //Bug: https://github.com/aspnet/WebFx/issues/244
            returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Home" : returnUrl;
            //if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            //else
            //{
            //    return RedirectToAction("Index", "Home");
            //}
        }

        #endregion
    }

    /// <summary>
    /// TODO: Temporary APIs to unblock build. Need to remove this once we have these APIs available. 
    /// </summary>
    public static class Extensions
    {
        public static string GetUserId(this IIdentity user)
        {
            return user.Name;
        }
    }

    /// <summary>
    /// TODO: Temporary APIs to unblock build. Need to remove this once we have these APIs available. 
    /// </summary>
    public static class DefaultAuthenticationTypes
    {
        public const string ApplicationCookie = "Application";
    }
}