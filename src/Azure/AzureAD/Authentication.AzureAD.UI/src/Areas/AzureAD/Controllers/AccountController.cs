// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureAD.Controllers.Internal
{
    [NonController]
    [AllowAnonymous]
    [Area("AzureAD")]
    [Route("[area]/[controller]/[action]")]
    internal class AccountController : Controller
    {
#pragma warning disable CS0618 // Type or member is obsolete
        public AccountController(IOptionsMonitor<AzureADOptions> options)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            Options = options;
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public IOptionsMonitor<AzureADOptions> Options { get; }
#pragma warning restore CS0618 // Type or member is obsolete

        [HttpGet("{scheme?}")]
        public IActionResult SignIn([FromRoute] string scheme)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            scheme = scheme ?? AzureADDefaults.AuthenticationScheme;
#pragma warning restore CS0618 // Type or member is obsolete
            var redirectUrl = Url.Content("~/");
            return Challenge(
                new AuthenticationProperties { RedirectUri = redirectUrl },
                scheme);
        }

        [HttpGet("{scheme?}")]
        public IActionResult SignOut([FromRoute] string scheme)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            scheme = scheme ?? AzureADDefaults.AuthenticationScheme;
#pragma warning restore CS0618 // Type or member is obsolete
            var options = Options.Get(scheme);
            var callbackUrl = Url.Page("/Account/SignedOut", pageHandler: null, values: null, protocol: Request.Scheme);
            return SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                options.CookieSchemeName,
                options.OpenIdConnectSchemeName);
        }
    }
}