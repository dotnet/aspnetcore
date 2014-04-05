using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using MusicStore.Models;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace MusicStore.Controllers
{
    //[Authorize]
    public class AccountController : Controller
    {
        public AccountController()
            //Bug: No EF yet - using an in memory store
            //: this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
            : this(new UserManager<ApplicationUser>(new InMemoryUserStore<ApplicationUser>()))
        {
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; private set; }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        //Bug: HTTP verb attribs not available
        //[HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid == true)
            {
                var user = await UserManager.FindByUserNamePassword(model.UserName, model.Password);
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
        public IActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        //Bug: Missing verb attributes
        //[HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid == true)
            {
                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignIn(user, isPersistent: false);
                    //Bug: No helper methods
                    //return RedirectToAction("Index", "Home");
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate
        //Bug: HTTP verbs
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Disassociate(string loginProvider, string providerKey)
        {

            ManageMessageId? message = null;
            var user = new ApplicationUser() { UserName = this.Context.User.Identity.GetUserId() };
            IdentityResult result = await UserManager.RemoveLogin(user, new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            //Bug: No helpers available
            //return RedirectToAction("Manage", new { Message = message });
            return View();
        }

        //
        // GET: /Account/Manage
        public IActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            //Bug: No Action method with single parameter
            //ViewBag.ReturnUrl = Url.Action("Manage");
            //ViewBag.ReturnUrl = Url.Action("Manage", "Account", null);
            return View();
        }

        //
        // POST: /Account/Manage
        //Bug: No verb attributes
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = await HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            //Bug: No Action method with single parameter
            //ViewBag.ReturnUrl = Url.Action("Manage");
            //ViewBag.ReturnUrl = Url.Action("Manage", "Account", null);
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
        // POST: /Account/ExternalLogin
        //Bug: No verb attributes
        //[HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await this.Context.Response.GetExternalLoginInfo();
            if (loginInfo == null)
            {
                //Bug: No helper
                //return RedirectToAction("Login");
                return View();
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindByLoginAsync(loginInfo.Login);
            if (user != null)
            {
                await SignIn(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
            }
        }

        //
        // POST: /Account/LinkLogin
        //Bug: No HTTP verbs
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account", null), this.Context.User.Identity.GetUserId());
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<IActionResult> LinkLoginCallback()
        {
            var loginInfo = await this.Context.Response.GetExternalLoginInfo(XsrfKey, this.Context.User.Identity.GetUserId());
            if (loginInfo == null)
            {
                //Bug: No helper method
                //return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
                return View();
            }
            var user = new ApplicationUser() { UserName = this.Context.User.Identity.GetUserId()};
            var result = await UserManager.AddLogin(user, loginInfo.Login);
            if (result.Succeeded)
            {
                //Bug: No helper method
                //return RedirectToAction("Manage");
                return View();
            }
            //Bug: No helper method
            //return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            return View();
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        //Bug: No HTTP verbs
        //[HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (this.Context.User.Identity.IsAuthenticated)
            {
                //Bug: No helper yet
                //return RedirectToAction("Manage");
                return View();
            }

            if (ModelState.IsValid == true)
            {
                // Get the information about the user from the external login provider
                var info = await this.Context.Response.GetExternalLoginInfo();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }

                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLogin(user, info.Login);
                    if (result.Succeeded)
                    {
                        await SignIn(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        //Bug: No HTTP verbs
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult LogOff()
        {
            this.Context.Response.SignOut();
            //Bug: No helper
            //return RedirectToAction("Index", "Home");
            return View();
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public IActionResult ExternalLoginFailure()
        {
            return View();
        }

        //Bug: Need this attribute
        //[ChildActionOnly]
        public async Task<IActionResult> RemoveAccountList()
        {
            var user = new ApplicationUser() { UserName = this.Context.User.Identity.GetUserId() };
            var linkedAccounts = await UserManager.GetLogins(user);
            ViewBag.ShowRemoveButton = await HasPassword() || linkedAccounts.Count > 1;
            //Bug: We dont have partial views yet
            //return (IActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
            return View();
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
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private async Task SignIn(ApplicationUser user, bool isPersistent)
        {
            this.Context.Response.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
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
            var user = await UserManager.FindByIdAsync(this.Context.User.Identity.GetUserId());
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
            //Bug: No helpers available
            //if (Url.IsLocalUrl(returnUrl))
            //{
            //    return Redirect(returnUrl);
            //}
            //else
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            return View();
        }

        private class ChallengeResult : HttpStatusCodeResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
                : base(401)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            new public void ExecuteResultAsync(ActionContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }

                context.HttpContext.Response.Challenge(LoginProvider, properties);
            }
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
            return string.Empty;
        }

        public static Task<ExternalLoginInfo> GetExternalLoginInfo(this HttpResponse response)
        {
            return Task.FromResult<ExternalLoginInfo>(new ExternalLoginInfo());
        }

        public static Task<ExternalLoginInfo> GetExternalLoginInfo(this HttpResponse response, string xsrfKey, string expectedValue)
        {
            return Task.FromResult<ExternalLoginInfo>(new ExternalLoginInfo());
        }
    }

    /// <summary>
    /// TODO: Temporary APIs to unblock build. Need to remove this once we have these APIs available. 
    /// </summary>
    public class ExternalLoginInfo
    {
        public string DefaultUserName { get; set; }
        public UserLoginInfo Login { get; set; }
    }

    /// <summary>
    /// TODO: Temporary APIs to unblock build. Need to remove this once we have these APIs available. 
    /// </summary>
    public static class DefaultAuthenticationTypes
    {
        public const string ApplicationCookie = "Application";
        public const string ExternalCookie = "External";
    }
}