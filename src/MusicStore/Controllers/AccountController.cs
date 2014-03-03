using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
#if NET45
using System.Security.Claims;
#else
using System.Security.ClaimsK;
#endif
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Mvc;
//using Microsoft.Owin.Security;
using MvcMusicStore.Models;

namespace MvcMusicStore.Controllers
{
    //[Authorize]
    public class AccountController : Controller
    {
        public AccountController()
            : this(new UserManager<ApplicationUser, string>(new InMemoryUserStore<ApplicationUser>()))
        {
        }

        public AccountController(UserManager<ApplicationUser, string> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser, string> UserManager { get; private set; }

        private void MigrateShoppingCart(string userName)
        {
            //var storeDb = new MusicStoreEntities();

            // Associate shopping cart items with logged-in user
            //var cart = ShoppingCart.GetCart(storeDb, this.HttpContext);
            //cart.MigrateCart(userName);
            //storeDb.SaveChanges();

            //Session[ShoppingCart.CartSessionKey] = userName;
        }

        //
        // GET: /Account/Login
        //[AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
        {
            //if (ModelState.IsValid)
            //{
                var user = await UserManager.Find(model.UserName, model.Password);
                if (user != null)
                {
                    await SignIn(user, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                //    ModelState.AddModelError("", "Invalid username or password.");
                }
            //}

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        //[AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //if (ModelState.IsValid)
            //{
                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.Create(user, model.Password);
                if (result.Succeeded)
                {
                    await SignIn(user, isPersistent: false);
                    return null;//RedirectToAction("Index", "Home");
                }
                else
                {
                    AddErrors(result);
                }
            //}

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLogin(null /*User.Identity.GetUserId()*/, new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return null;//RedirectToAction("Manage", new { Message = message });
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
            //ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            //ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                //if (ModelState.IsValid)
                //{
                    IdentityResult result = await UserManager.ChangePassword("userId" /*User.Identity.GetUserId()*/, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return null;//RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                //}
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                //ModelState state = ModelState["OldPassword"];
                //if (state != null)
                //{
                //    state.Errors.Clear();
                //}

                //if (ModelState.IsValid)
                //{
                    IdentityResult result = await UserManager.AddPassword("userId" /*User.Identity.GetUserId()*/, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return null;//RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                //}
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            //return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
            return null;
        }

        //
        // GET: /Account/ExternalLoginCallback
        //[AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
        {
            //var loginInfo = await AuthenticationManager.GetExternalLoginInfo();
            //if (loginInfo == null)
            //{
            //    return RedirectToAction("Login");
            //}

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.Find(null/*loginInfo.Login*/);
            if (user != null)
            {
                await SignIn(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = null;//loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { /*UserName = loginInfo.DefaultUserName*/ });
            }
        }

        //
        // POST: /Account/LinkLogin
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            //return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
            return null;
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<IActionResult> LinkLoginCallback()
        {
            //var loginInfo = await AuthenticationManager.GetExternalLoginInfo(XsrfKey, User.Identity.GetUserId());
            //if (loginInfo == null)
            //{
            //    return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            //}
            //var result = await UserManager.AddLogin(User.Identity.GetUserId(), loginInfo.Login);
            //if (result.Succeeded)
            //{
            //    return RedirectToAction("Manage");
            //}
            return null;//RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            //if (User.Identity.IsAuthenticated)
            //{
            //    return RedirectToAction("Manage");
            //}

            //if (ModelState.IsValid)
            //{
                // Get the information about the user from the external login provider
                //var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                //if (info == null)
                //{
                //    return View("ExternalLoginFailure");
                //}
                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.Create(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLogin(user.Id, null/*info.Login*/);
                    if (result.Succeeded)
                    {
                        await SignIn(user, isPersistent: false);
                        return null;//RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            //}

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult LogOff()
        {
            //AuthenticationManager.SignOut();
            return null;//RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        //[AllowAnonymous]
        public IActionResult ExternalLoginFailure()
        {
            return View();
        }

        //[ChildActionOnly]
        public IActionResult RemoveAccountList()
        {
            //var linkedAccounts = UserManager.GetLogins(null /*User.Identity.GetUserId()*/);
            //ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return null;//(IActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        //private IAuthenticationManager AuthenticationManager
        //{
        //    get
        //    {
        //        return HttpContext.GetOwinContext().Authentication;
        //    }
        //}

        private async Task SignIn(ApplicationUser user, bool isPersistent)
        {
            //AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentity(user, "Application" /*DefaultAuthenticationTypes.ApplicationCookie */);
            //AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);

            // Migrate the user's shopping cart
            MigrateShoppingCart(user.UserName);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                //ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            // No sync helpers yet
            //var user = UserManager.FindById(null /*User.Identity.GetUserId()*/);
            //if (user != null)
            //{
            //    return user.PasswordHash != null;
            //}
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
            //if (Url.IsLocalUrl(returnUrl))
            //{
            //    return Redirect(returnUrl);
            //}
            //else
            //{
            //    return RedirectToAction("Index", "Home");
            //}
            return null;
        }

        //private class ChallengeResult : HttpUnauthorizedResult
        //{
        //    public ChallengeResult(string provider, string redirectUri)
        //        : this(provider, redirectUri, null)
        //    {
        //    }

        //    public ChallengeResult(string provider, string redirectUri, string userId)
        //    {
        //        LoginProvider = provider;
        //        RedirectUri = redirectUri;
        //        UserId = userId;
        //    }

        //    public string LoginProvider { get; set; }
        //    public string RedirectUri { get; set; }
        //    public string UserId { get; set; }

        //    public override void ExecuteResult(ControllerContext context)
        //    {
        //        var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
        //        if (UserId != null)
        //        {
        //            properties.Dictionary[XsrfKey] = UserId;
        //        }
        //        context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
        //    }
        //}
        #endregion
    }
}