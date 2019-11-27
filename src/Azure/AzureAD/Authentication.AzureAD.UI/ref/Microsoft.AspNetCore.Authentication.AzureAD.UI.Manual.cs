// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureAD.Controllers.Internal
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    [Microsoft.AspNetCore.Mvc.AreaAttribute("AzureAD")]
    [Microsoft.AspNetCore.Mvc.NonControllerAttribute]
    [Microsoft.AspNetCore.Mvc.RouteAttribute("[area]/[controller]/[action]")]
    internal partial class AccountController : Microsoft.AspNetCore.Mvc.Controller
    {
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        public AccountController(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureADOptions> options) { }
        public Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureADOptions> Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [Microsoft.AspNetCore.Mvc.HttpGetAttribute("{scheme?}")]
        public Microsoft.AspNetCore.Mvc.IActionResult SignIn([Microsoft.AspNetCore.Mvc.FromRouteAttribute]string scheme) { throw null; }
        [Microsoft.AspNetCore.Mvc.HttpGetAttribute("{scheme?}")]
        public Microsoft.AspNetCore.Mvc.IActionResult SignOut([Microsoft.AspNetCore.Mvc.FromRouteAttribute]string scheme) { throw null; }
    }
}
