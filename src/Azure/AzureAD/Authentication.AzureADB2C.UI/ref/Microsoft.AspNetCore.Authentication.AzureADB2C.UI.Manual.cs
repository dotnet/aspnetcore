// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    internal partial class AzureADB2COpenIDConnectEventHandlers
    {
        public AzureADB2COpenIDConnectEventHandlers(string schemeName, Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions options) { }
        public Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string SchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Threading.Tasks.Task OnRedirectToIdentityProvider(Microsoft.AspNetCore.Authentication.OpenIdConnect.RedirectContext context) { throw null; }
        public System.Threading.Tasks.Task OnRemoteFailure(Microsoft.AspNetCore.Authentication.RemoteFailureContext context) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2C.Controllers.Internal
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    [Microsoft.AspNetCore.Mvc.AreaAttribute("AzureADB2C")]
    [Microsoft.AspNetCore.Mvc.NonControllerAttribute]
    [Microsoft.AspNetCore.Mvc.RouteAttribute("[area]/[controller]/[action]")]
    internal partial class AccountController : Microsoft.AspNetCore.Mvc.Controller
    {
        public AccountController(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions> AzureADB2COptions) { }
        [Microsoft.AspNetCore.Mvc.HttpGetAttribute("{scheme?}")]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> EditProfile([Microsoft.AspNetCore.Mvc.FromRouteAttribute]string scheme) { throw null; }
        [Microsoft.AspNetCore.Mvc.HttpGetAttribute("{scheme?}")]
        public Microsoft.AspNetCore.Mvc.IActionResult ResetPassword([Microsoft.AspNetCore.Mvc.FromRouteAttribute]string scheme) { throw null; }
        [Microsoft.AspNetCore.Mvc.HttpGetAttribute("{scheme?}")]
        public Microsoft.AspNetCore.Mvc.IActionResult SignIn([Microsoft.AspNetCore.Mvc.FromRouteAttribute]string scheme) { throw null; }
        [Microsoft.AspNetCore.Mvc.HttpGetAttribute("{scheme?}")]
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.IActionResult> SignOut([Microsoft.AspNetCore.Mvc.FromRouteAttribute]string scheme) { throw null; }
    }
}
