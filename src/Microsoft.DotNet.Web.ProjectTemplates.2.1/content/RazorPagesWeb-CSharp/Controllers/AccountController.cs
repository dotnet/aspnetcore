using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif
#if (IndividualLocalAuth)
using Microsoft.AspNetCore.Identity;
#endif
using Microsoft.AspNetCore.Mvc;
#if (IndividualLocalAuth)
using Microsoft.Extensions.Logging;
using Company.WebApplication1.Data;
#endif
#if (IndividualB2CAuth)
using Microsoft.Extensions.Options;
#endif

namespace Company.WebApplication1.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
#if (IndividualLocalAuth)
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;

        public AccountController(SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToPage("/Index");
        }
#elseif (OrganizationalAuth)
        [HttpGet]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Page("/Index");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }

        [HttpGet]
        public IActionResult SignOut()
        {
            var callbackUrl = Url.Page("/Account/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme);
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme
            );
        }
#elseif (IndividualB2CAuth)
        private readonly AzureAdB2COptions _options;

        public AccountController(IOptions<AzureAdB2COptions> b2cOptions)
        {
            _options = b2cOptions.Value;
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            var redirectUrl = Url.Page("/Index");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var redirectUrl = Url.Page("/Index");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[AzureAdB2COptions.PolicyAuthenticationProperty] = _options.ResetPasswordPolicyId;
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            var redirectUrl = Url.Page("/Index");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items[AzureAdB2COptions.PolicyAuthenticationProperty] = _options.EditProfilePolicyId;
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public IActionResult SignOut()
        {
            var callbackUrl = Url.Page("/Account/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme);
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme
            );
        }
#endif
    }
}
