// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.InMemory;
//using Microsoft.AspNet.Mvc;
//using Microsoft.AspNet.Mvc.ModelBinding;
//using MusicStore.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace MusicStore.Controllers
//{
//    //[Authorize]
//    public class AccountController : Controller
//    {
//        public AccountController()
//            //Bug: No EF yet - using an in memory store
//            //: this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
//            : this(new UserManager<ApplicationUser, string>(new InMemoryUserStore<ApplicationUser>()))
//        {
//        }

//        public AccountController(UserManager<ApplicationUser, string> userManager)
//        {
//            UserManager = userManager;
//        }

//        public UserManager<ApplicationUser, string> UserManager { get; private set; }

//        private void MigrateShoppingCart(string UserName)
//        {
//            //Bug: No EF
//            //var storeDb = new MusicStoreEntities();
//            var storeDb = MusicStoreEntities.Instance;

//            // Associate shopping cart items with logged-in user
//            var cart = ShoppingCart.GetCart(storeDb, this.Context);
//            cart.MigrateCart(UserName);
//            storeDb.SaveChanges();

//            //Bug: TODO
//            //Session[ShoppingCart.CartSessionKey] = UserName;
//        }

//        //
//        // GET: /Account/Login
//        [AllowAnonymous]
//        public IActionResult Login(string returnUrl)
//        {
//            //ViewBag.ReturnUrl = returnUrl;
//            return View();
//        }

//        //
//        // POST: /Account/Login
//        //Bug: HTTP verb attribs not available
//        //[HttpPost]
//        [AllowAnonymous]
//        //[ValidateAntiForgeryToken]
//        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
//        {
//            //Bug: How to validate the model state?
//            //if (ModelState.IsValid)
//            {
//                var user = await UserManager.Find(model.UserName, model.Password);
//                if (user != null)
//                {
//                    await SignIn(user, model.RememberMe);
//                    return RedirectToLocal(returnUrl);
//                }
//                else
//                {
//                    //Bug: Model state error
//                    //ModelState.AddModelError("", "Invalid username or password.");
//                }
//            }

//            // If we got this far, something failed, redisplay form
//            return View(model);
//        }

//        //
//        // GET: /Account/Register
//        [AllowAnonymous]
//        public IActionResult Register()
//        {
//            return View();
//        }

//        //
//        // POST: /Account/Register
//        //Bug: Missing verb attributes
//        //[HttpPost]
//        [AllowAnonymous]
//        //[ValidateAntiForgeryToken]
//        public async Task<IActionResult> Register(RegisterViewModel model)
//        {
//            //Bug: How to validate the model state?
//            //if (ModelState.IsValid)
//            {
//                //Bug: Replacing it with InmemoryUser
//                var user = new ApplicationUser() { UserName = model.UserName };
//                var result = await UserManager.Create(user, model.Password);
//                if (result.Succeeded)
//                {
//                    await SignIn(user, isPersistent: false);
//                    //Bug: No helper methods
//                    //return RedirectToAction("Index", "Home");
//                }
//                else
//                {
//                    AddErrors(result);
//                }
//            }

//            // If we got this far, something failed, redisplay form
//            return View(model);
//        }

//        //
//        // POST: /Account/Disassociate
//        //Bug: HTTP verbs
//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        public async Task<IActionResult> Disassociate(string loginProvider, string providerKey)
//        {
//            ManageMessageId? message = null;
//            IdentityResult result = await UserManager.RemoveLogin(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
//            if (result.Succeeded)
//            {
//                message = ManageMessageId.RemoveLoginSuccess;
//            }
//            else
//            {
//                message = ManageMessageId.Error;
//            }
//            //Bug: No helpers available
//            //return RedirectToAction("Manage", new { Message = message });
//            return View();
//        }

//        //
//        // GET: /Account/Manage
//        public IActionResult Manage(ManageMessageId? message)
//        {
//            //ViewBag.StatusMessage =
//            //    message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
//            //    : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
//            //    : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
//            //    : message == ManageMessageId.Error ? "An error has occurred."
//            //    : "";
//            //ViewBag.HasLocalPassword = HasPassword();
//            //Bug: No Action method with single parameter
//            //ViewBag.ReturnUrl = Url.Action("Manage");
//            //ViewBag.ReturnUrl = Url.Action("Manage", "Account", null);
//            return View();
//        }

//        //
//        // POST: /Account/Manage
//        //Bug: No verb attributes
//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        public async Task<IActionResult> Manage(ManageUserViewModel model)
//        {
//            bool hasPassword = await HasPassword();
//            //ViewBag.HasLocalPassword = hasPassword;
//            //Bug: No Action method with single parameter
//            //ViewBag.ReturnUrl = Url.Action("Manage");
//            //ViewBag.ReturnUrl = Url.Action("Manage", "Account", null);
//            if (hasPassword)
//            {
//                //if (ModelState.IsValid)
//                {
//                    IdentityResult result = await UserManager.ChangePassword(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
//                    if (result.Succeeded)
//                    {
//                        //Bug: No helper method
//                        //return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
//                        return View();
//                    }
//                    else
//                    {
//                        AddErrors(result);
//                    }
//                }
//            }
//            else
//            {
//                // User does not have a password so remove any validation errors caused by a missing OldPassword field
//                //Bug: Still controller does not have a ModelState property
//                //ModelState state = ModelState["OldPassword"];
//                ModelState state = null;

//                if (state != null)
//                {
//                    state.Errors.Clear();
//                }

//                //Bug: No model state validation
//                //if (ModelState.IsValid)
//                {
//                    IdentityResult result = await UserManager.AddPassword(User.Identity.GetUserId(), model.NewPassword);
//                    if (result.Succeeded)
//                    {
//                        //Bug: No helper method
//                        //return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
//                    }
//                    else
//                    {
//                        AddErrors(result);
//                    }
//                }
//            }

//            // If we got this far, something failed, redisplay form
//            return View(model);
//        }

//        //
//        // POST: /Account/ExternalLogin
//        //Bug: No verb attributes
//        //[HttpPost]
//        [AllowAnonymous]
//        //[ValidateAntiForgeryToken]
//        public IActionResult ExternalLogin(string provider, string returnUrl)
//        {
//            // Request a redirect to the external login provider
//            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
//        }

//        //
//        // GET: /Account/ExternalLoginCallback
//        [AllowAnonymous]
//        public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
//        {
//            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
//            if (loginInfo == null)
//            {
//                //Bug: No helper
//                //return RedirectToAction("Login");
//                return View();
//            }

//            // Sign in the user with this external login provider if the user already has a login
//            var user = await UserManager.Find(loginInfo.Login);
//            if (user != null)
//            {
//                await SignIn(user, isPersistent: false);
//                return RedirectToLocal(returnUrl);
//            }
//            else
//            {
//                // If the user does not have an account, then prompt the user to create an account
//                //ViewBag.ReturnUrl = returnUrl;
//                //ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
//                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
//            }
//        }

//        //
//        // POST: /Account/LinkLogin
//        //Bug: No HTTP verbs
//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        public IActionResult LinkLogin(string provider)
//        {
//            // Request a redirect to the external login provider to link a login for the current user
//            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account", null), User.Identity.GetUserId());
//        }

//        //
//        // GET: /Account/LinkLoginCallback
//        public async Task<IActionResult> LinkLoginCallback()
//        {
//            var loginInfo = null;// await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
//            if (loginInfo == null)
//            {
//                //Bug: No helper method
//                //return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
//                return View();
//            }
//            var result = await UserManager.AddLogin(User.Identity.GetUserId(), loginInfo.Login);
//            if (result.Succeeded)
//            {
//                //Bug: No helper method
//                //return RedirectToAction("Manage");
//                return View();
//            }
//            //Bug: No helper method
//            //return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
//            return View();
//        }

//        //
//        // POST: /Account/ExternalLoginConfirmation
//        //Bug: No HTTP verbs
//        //[HttpPost]
//        [AllowAnonymous]
//        //[ValidateAntiForgeryToken]
//        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
//        {
//            if (User.Identity.IsAuthenticated)
//            {
//                //Bug: No helper yet
//                //return RedirectToAction("Manage");
//                return View();
//            }

//            //Bug: No model state validation
//            //if (ModelState.IsValid)
//            {
//                // Get the information about the user from the external login provider
//                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
//                if (info == null)
//                {
//                    return View("ExternalLoginFailure");
//                }
//                //Using InMemory user
//                var user = new ApplicationUser() { UserName = model.UserName };
//                var result = await UserManager.Create(user);
//                if (result.Succeeded)
//                {
//                    result = await UserManager.AddLogin(user.Id, info.Login);
//                    if (result.Succeeded)
//                    {
//                        await SignIn(user, isPersistent: false);
//                        return RedirectToLocal(returnUrl);
//                    }
//                }
//                AddErrors(result);
//            }

//            //ViewBag.ReturnUrl = returnUrl;
//            return View(model);
//        }

//        //
//        // POST: /Account/LogOff
//        //Bug: No HTTP verbs
//        //[HttpPost]
//        //[ValidateAntiForgeryToken]
//        public IActionResult LogOff()
//        {
//            AuthenticationManager.SignOut();
//            //return RedirectToAction("Index", "Home");
//            return View();
//        }

//        //
//        // GET: /Account/ExternalLoginFailure
//        [AllowAnonymous]
//        public IActionResult ExternalLoginFailure()
//        {
//            return View();
//        }

//        //Bug: Need this attribute
//        //[ChildActionOnly]
//        public async Task<IActionResult> RemoveAccountList()
//        {
//            var linkedAccounts = await UserManager.GetLogins(User.Identity.GetUserId());
//            //ViewBag.ShowRemoveButton = await HasPassword() || linkedAccounts.Count > 1;
//            //Bug: We dont have partial views yet
//            //return (IActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
//            return View();
//        }

//        //Bug: Controllers need to be disposable? 
//        protected void Dispose(bool disposing)
//        {
//            if (disposing && UserManager != null)
//            {
//                UserManager.Dispose();
//                UserManager = null;
//            }
            
//            //base.Dispose(disposing);
//        }

//        #region Helpers
//        // Used for XSRF protection when adding external logins
//        private const string XsrfKey = "XsrfId";

//        //private IAuthenticationManager AuthenticationManager
//        //{
//        //    get
//        //    {
//        //        //Will change to Context.Authentication
//        //        return new IAuthenticationManager();
//        //    }
//        //}

//        private async Task SignIn(ApplicationUser user, bool isPersistent)
//        {
//            //Bug: No cookies middleware now.
//            //AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
//            //var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
//            //AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);

//            // Migrate the user's shopping cart
//            MigrateShoppingCart(user.UserName);
//        }

//        private void AddErrors(IdentityResult result)
//        {
//            foreach (var error in result.Errors)
//            {
//                //ModelState.AddModelError("", error);
//            }
//        }

//        private async Task<bool> HasPassword()
//        {
//            //Bug: Need to get the User object somehow: TODO
//            //var user = await UserManager.FindById(User.Identity.GetUserId());
//            var user = await UserManager.FindById("TODO");
//            if (user != null)
//            {
//                return user.PasswordHash != null;
//            }
//            return false;
//        }

//        public enum ManageMessageId
//        {
//            ChangePasswordSuccess,
//            SetPasswordSuccess,
//            RemoveLoginSuccess,
//            Error
//        }

//        private IActionResult RedirectToLocal(string returnUrl)
//        {
//            //Bug: No helpers available
//            //if (Url.IsLocalUrl(returnUrl))
//            //{
//            //    return Redirect(returnUrl);
//            //}
//            //else
//            //{
//            //    return RedirectToAction("Index", "Home");
//            //}
//            return View();
//        }

//        private class ChallengeResult : HttpStatusCodeResult
//        {
//            public ChallengeResult(string provider, string redirectUri)
//                : this(provider, redirectUri, null)
//            {
//            }

//            public ChallengeResult(string provider, string redirectUri, string userId)
//                : base(401)
//            {
//                LoginProvider = provider;
//                RedirectUri = redirectUri;
//                UserId = userId;
//            }

//            public string LoginProvider { get; set; }
//            public string RedirectUri { get; set; }
//            public string UserId { get; set; }

//            new public void ExecuteResultAsync(ActionContext context)
//            {
//                //Bug: No security package yet
//                //var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
//                //if (UserId != null)
//                //{
//                //    properties.Dictionary[XsrfKey] = UserId;
//                //}
//                //context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
//            }
//        }
//        #endregion
//    }

//    //Bug: To remove this. Until we have ClaimsPrincipal available
//    internal class User
//    {
//        public static IdentityInstance Identity { get; set; }

//        public User()
//        {
//            if (Identity == null)
//            {
//                Identity = new IdentityInstance();
//            }
//        }

//        internal class IdentityInstance
//        {
//            public string GetUserId()
//            {
//                return string.Empty;
//            }

//            public bool IsAuthenticated { get; set; }
//        }
//    }
//}