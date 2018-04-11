// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.UI.Pages.Account.Internal
{
    [AllowAnonymous]
    [IdentityDefaultUI(typeof(LogoutModel<>))]
    public abstract class LogoutModel : PageModel
    {
        public void OnGet()
        {
        }

        public virtual Task<IActionResult> OnPost(string returnUrl = null) => throw new NotImplementedException();
    }

    internal class LogoutModel<TUser> : LogoutModel where TUser : class
    {
        private readonly SignInManager<TUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<TUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public override async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
