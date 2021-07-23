// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureAD.Controllers.Internal
{
    [NonController]
    [AllowAnonymous]
    [Area("AzureAD")]
    [Route("[area]/[controller]/[action]")]
    [Obsolete("This is obsolete and will be removed in a future version. Use Microsoft.Identity.Web instead. See https://aka.ms/ms-identity-web.")]
    internal class AccountController : Controller
    {
        public AccountController(IOptionsMonitor<AzureADOptions> options)
        {
            Options = options;
        }

        public IOptionsMonitor<AzureADOptions> Options { get; }

        [HttpGet("{scheme?}")]
        public IActionResult SignIn([FromRoute] string scheme)
        {
            scheme = scheme ?? AzureADDefaults.AuthenticationScheme;
            var redirectUrl = Url.Content("~/");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                scheme);
        }

        [HttpGet("{scheme?}")]
        public IActionResult SignOut([FromRoute] string scheme)
        {
            scheme = scheme ?? AzureADDefaults.AuthenticationScheme;
            var options = Options.Get(scheme);
            var callbackUrl = Url.Page("/Account/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme);
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                options.CookieSchemeName,
                options.OpenIdConnectSchemeName);
        }
    }
}