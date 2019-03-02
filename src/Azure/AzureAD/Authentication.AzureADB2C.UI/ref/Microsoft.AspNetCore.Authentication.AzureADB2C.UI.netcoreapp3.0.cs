// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication
{
    public static partial class AzureADB2CAuthenticationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureADB2C(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureADB2C(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string scheme, string openIdConnectScheme, string cookieScheme, string displayName, System.Action<Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureADB2CBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, System.Action<Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions> configureOptions) { throw null; }
        public static Microsoft.AspNetCore.Authentication.AuthenticationBuilder AddAzureADB2CBearer(this Microsoft.AspNetCore.Authentication.AuthenticationBuilder builder, string scheme, string jwtBearerScheme, System.Action<Microsoft.AspNetCore.Authentication.AzureADB2C.UI.AzureADB2COptions> configureOptions) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    public static partial class AzureADB2CDefaults
    {
        public const string AuthenticationScheme = "AzureADB2C";
        public const string BearerAuthenticationScheme = "AzureADB2CBearer";
        public const string CookieScheme = "AzureADB2CCookie";
        public static readonly string DisplayName;
        public const string JwtBearerAuthenticationScheme = "AzureADB2CJwtBearer";
        public const string OpenIdScheme = "AzureADB2COpenID";
        public static readonly string PolicyKey;
    }
    public partial class AzureADB2COptions
    {
        public AzureADB2COptions() { }
        public string[] AllSchemes { get { throw null; } }
        public string CallbackPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ClientId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ClientSecret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string CookieSchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string DefaultPolicy { get { throw null; } }
        public string Domain { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string EditProfilePolicyId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string JwtBearerSchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string OpenIdConnectSchemeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ResetPasswordPolicyId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SignedOutCallbackPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SignUpSignInPolicyId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI.Internal
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
