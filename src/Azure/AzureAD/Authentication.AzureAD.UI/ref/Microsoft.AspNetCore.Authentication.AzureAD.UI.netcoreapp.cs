// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public static partial class AzureADAuthenticationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureAD(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureADOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureAD(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string scheme, string openIdConnectScheme, string cookieScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureADOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureADBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureADOptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureADBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string scheme, string jwtBearerScheme, System.Action<Microsoft.AspNetCore.Authentication.AzureAD.UI.AzureADOptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Authentication.AzureAD.UI
{
    public static partial class AzureADDefaults
    {
        public const string AuthenticationScheme = "AzureAD";
        public const string BearerAuthenticationScheme = "AzureADBearer";
        public const string CookieScheme = "AzureADCookie";
        public static readonly string DisplayName;
        public const string JwtBearerAuthenticationScheme = "AzureADJwtBearer";
        public const string OpenIdScheme = "AzureADOpenID";
    }
    public partial class AzureADOptions
    {
        public AzureADOptions() { }
        public string[] AllSchemes { get { throw null; } }
        public string CallbackPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ClientId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ClientSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string CookieSchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Domain { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string JwtBearerSchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string OpenIdConnectSchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SignedOutCallbackPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TenantId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Authentication.AzureAD.UI.Internal
{
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class AccessDeniedModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public AccessDeniedModel() { }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    [Microsoft.AspNetCore.Mvc.ResponseCacheAttribute(Duration=0, Location=Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None, NoStore=true)]
    public partial class ErrorModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public ErrorModel() { }
        public string RequestId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ShowRequestId { get { throw null; } }
        public void OnGet() { }
    }
    [Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute]
    public partial class SignedOutModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public SignedOutModel() { }
        public Microsoft.AspNetCore.Mvc.IActionResult OnGet() { throw null; }
    }
}
