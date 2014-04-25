using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Security;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using MusicStore.Models;
using System.Security.Principal;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    //https://github.com/aspnet/WebFx/issues/309
    //[Authorize]
    public class AccountController : Controller
    {
        public UserManager<ApplicationUser> UserManager
        {
            get { return Context.ApplicationServices.GetService<ApplicationUserManager>(); }
        }

        private SignInManager<ApplicationUser> _signInManager;
        public SignInManager<ApplicationUser> SignInManager
        {
            get
            {
                if (_signInManager == null)
                {
                    _signInManager = new SignInManager<ApplicationUser>
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
                    return Json(result.Errors);
                    //https://github.com/aspnet/WebFx/issues/289
                    //AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Manage
        [HttpGet]
        //Bug: https://github.com/aspnet/WebFx/issues/256. Unable to model bind for a nullable value. Work around to remove the nullable
        //public async Task<IActionResult> Manage(ManageMessageId? message)
        public async Task<IActionResult> Manage(ManageMessageId message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageUserViewModel model)
        {
            ViewBag.ReturnUrl = Url.Action("Manage");
            //Bug: https://github.com/aspnet/DataAnnotations/issues/21
            //if (ModelState.IsValid == true)
            {
                var user = await GetCurrentUserAsync();
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

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult LogOff()
        {
            SignInManager.SignOut();
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

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await UserManager.FindByIdAsync(Context.User.Identity.GetUserId());
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            Error
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            //Bug: https://github.com/aspnet/WebFx/issues/244
            returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Home" : returnUrl;
            //if (Url.IsLocalUrl(returnUrl))
            if (true)
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
}