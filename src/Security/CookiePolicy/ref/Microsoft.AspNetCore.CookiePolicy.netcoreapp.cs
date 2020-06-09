// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class CookiePolicyAppBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseCookiePolicy(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseCookiePolicy(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.CookiePolicyOptions options) { throw null; }
    }
    public partial class CookiePolicyOptions
    {
        public CookiePolicyOptions() { }
        public System.Func<Microsoft.AspNetCore.Http.HttpContext, bool> CheckConsentNeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.CookieBuilder ConsentCookie { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy HttpOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.SameSiteMode MinimumSameSitePolicy { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Action<Microsoft.AspNetCore.CookiePolicy.AppendCookieContext> OnAppendCookie { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Action<Microsoft.AspNetCore.CookiePolicy.DeleteCookieContext> OnDeleteCookie { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.CookieSecurePolicy Secure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.AspNetCore.CookiePolicy
{
    public partial class AppendCookieContext
    {
        public AppendCookieContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.CookieOptions options, string name, string value) { }
        public Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string CookieName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.CookieOptions CookieOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string CookieValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool HasConsent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsConsentNeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IssueCookie { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class CookiePolicyMiddleware
    {
        public CookiePolicyMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.CookiePolicyOptions> options) { }
        public CookiePolicyMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.CookiePolicyOptions> options, Microsoft.Extensions.Logging.ILoggerFactory factory) { }
        public Microsoft.AspNetCore.Builder.CookiePolicyOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class DeleteCookieContext
    {
        public DeleteCookieContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.AspNetCore.Http.CookieOptions options, string name) { }
        public Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string CookieName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.CookieOptions CookieOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool HasConsent { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsConsentNeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IssueCookie { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public enum HttpOnlyPolicy
    {
        None = 0,
        Always = 1,
    }
}
