using System;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Security;
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
            : this(new UserManager<ApplicationUser>(Startup.UserStore))
        {
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; set; }

        private SignInManager<ApplicationUser> _signInManager;
        public SignInManager<ApplicationUser> SignInManager {
            get
            {
                if (_signInManager == null)
                {
                    _signInManager = new SignInManager<ApplicationUser>()
                    {
                        UserManager = UserManager,
                        Context = Context,
                        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie
                    };
                }
                return _signInManager;
            }
            set { _signInManager = value; } 
        }

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
                var signInStatus = await SignInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, shouldLockout: false);
                switch (signInStatus)
                {
                    case SignInStatus.Success:
                        return RedirectToLocal(returnUrl);
                    case SignInStatus.LockedOut:
                        ModelState.AddModelError("", "User is locked out, try again later.");
                        return View(model);
                    case SignInStatus.Failure:
                    default:
                        ModelState.AddModelError("", "Invalid username or password.");
                        return View(model);
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
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    return RedirectToAction("Index", "Home");
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
                    var user = await GetCurrentUser();
                    var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
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
                    var user = await GetCurrentUser();
                    var result = await UserManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
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
            // Bug: This should call SignInManager.SignOut() once its available
            this.Context.Response.SignOut();
            return RedirectToAction("Index", "Home");
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {
            return await UserManager.FindByIdAsync(Context.User.Identity.GetUserId());
        }

        private async Task<bool> HasPassword()
        {
            var user = await GetCurrentUser();
            if (user != null)
            {
                return await UserManager.HasPasswordAsync(user);
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
            if(true)
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }

    /// <summary>
    /// TODO: Temporary APIs to unblock build. Need to remove this once we have these APIs available. 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Return the user name using the UserNameClaimType
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public static string GetUserName(this IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var ci = identity as ClaimsIdentity;
            if (ci != null)
            {
                return ci.FindFirstValue(ClaimsIdentity.DefaultNameClaimType);
            }
            return null;
        }

        /// <summary>
        ///     Return the user id using the UserIdClaimType
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public static string GetUserId(this IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var ci = identity as ClaimsIdentity;
            if (ci != null)
            {
                return ci.FindFirstValue(ClaimTypes.NameIdentifier);
            }
            return null;
        }

        /// <summary>
        ///     Return the claim value for the first claim with the specified type if it exists, null otherwise
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="claimType"></param>
        /// <returns></returns>
        public static string FindFirstValue(this ClaimsIdentity identity, string claimType)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var claim = identity.FindFirst(claimType);
            return claim != null ? claim.Value : null;
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