// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRequestLocalization(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRequestLocalization(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.RequestLocalizationOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRequestLocalization(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.Builder.RequestLocalizationOptions> optionsAction) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRequestLocalization(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, params string[] cultures) { throw null; }
    }
    public partial class RequestLocalizationOptions
    {
        public RequestLocalizationOptions() { }
        public bool ApplyCurrentCultureToResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Localization.RequestCulture DefaultRequestCulture { get { throw null; } set { } }
        public bool FallBackToParentCultures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool FallBackToParentUICultures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Localization.IRequestCultureProvider> RequestCultureProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<System.Globalization.CultureInfo> SupportedCultures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<System.Globalization.CultureInfo> SupportedUICultures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Builder.RequestLocalizationOptions AddSupportedCultures(params string[] cultures) { throw null; }
        public Microsoft.AspNetCore.Builder.RequestLocalizationOptions AddSupportedUICultures(params string[] uiCultures) { throw null; }
        public Microsoft.AspNetCore.Builder.RequestLocalizationOptions SetDefaultCulture(string defaultCulture) { throw null; }
    }
    public static partial class RequestLocalizationOptionsExtensions
    {
        public static Microsoft.AspNetCore.Builder.RequestLocalizationOptions AddInitialRequestCultureProvider(this Microsoft.AspNetCore.Builder.RequestLocalizationOptions requestLocalizationOptions, Microsoft.AspNetCore.Localization.RequestCultureProvider requestCultureProvider) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Localization
{
    public partial class AcceptLanguageHeaderRequestCultureProvider : Microsoft.AspNetCore.Localization.RequestCultureProvider
    {
        public AcceptLanguageHeaderRequestCultureProvider() { }
        public int MaximumAcceptLanguageHeaderValuesToTry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class CookieRequestCultureProvider : Microsoft.AspNetCore.Localization.RequestCultureProvider
    {
        public static readonly string DefaultCookieName;
        public CookieRequestCultureProvider() { }
        public string CookieName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
        public static string MakeCookieValue(Microsoft.AspNetCore.Localization.RequestCulture requestCulture) { throw null; }
        public static Microsoft.AspNetCore.Localization.ProviderCultureResult ParseCookieValue(string value) { throw null; }
    }
    public partial class CustomRequestCultureProvider : Microsoft.AspNetCore.Localization.RequestCultureProvider
    {
        public CustomRequestCultureProvider(System.Func<Microsoft.AspNetCore.Http.HttpContext, System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult>> provider) { }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial interface IRequestCultureFeature
    {
        Microsoft.AspNetCore.Localization.IRequestCultureProvider Provider { get; }
        Microsoft.AspNetCore.Localization.RequestCulture RequestCulture { get; }
    }
    public partial interface IRequestCultureProvider
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    public partial class ProviderCultureResult
    {
        public ProviderCultureResult(Microsoft.Extensions.Primitives.StringSegment culture) { }
        public ProviderCultureResult(Microsoft.Extensions.Primitives.StringSegment culture, Microsoft.Extensions.Primitives.StringSegment uiCulture) { }
        public ProviderCultureResult(System.Collections.Generic.IList<Microsoft.Extensions.Primitives.StringSegment> cultures) { }
        public ProviderCultureResult(System.Collections.Generic.IList<Microsoft.Extensions.Primitives.StringSegment> cultures, System.Collections.Generic.IList<Microsoft.Extensions.Primitives.StringSegment> uiCultures) { }
        public System.Collections.Generic.IList<Microsoft.Extensions.Primitives.StringSegment> Cultures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.Extensions.Primitives.StringSegment> UICultures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class QueryStringRequestCultureProvider : Microsoft.AspNetCore.Localization.RequestCultureProvider
    {
        public QueryStringRequestCultureProvider() { }
        public string QueryStringKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string UIQueryStringKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class RequestCulture
    {
        public RequestCulture(System.Globalization.CultureInfo culture) { }
        public RequestCulture(System.Globalization.CultureInfo culture, System.Globalization.CultureInfo uiCulture) { }
        public RequestCulture(string culture) { }
        public RequestCulture(string culture, string uiCulture) { }
        public System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Globalization.CultureInfo UICulture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class RequestCultureFeature : Microsoft.AspNetCore.Localization.IRequestCultureFeature
    {
        public RequestCultureFeature(Microsoft.AspNetCore.Localization.RequestCulture requestCulture, Microsoft.AspNetCore.Localization.IRequestCultureProvider provider) { }
        public Microsoft.AspNetCore.Localization.IRequestCultureProvider Provider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Localization.RequestCulture RequestCulture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class RequestCultureProvider : Microsoft.AspNetCore.Localization.IRequestCultureProvider
    {
        protected static readonly System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> NullProviderCultureResult;
        protected RequestCultureProvider() { }
        public Microsoft.AspNetCore.Builder.RequestLocalizationOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public abstract System.Threading.Tasks.Task<Microsoft.AspNetCore.Localization.ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext);
    }
    public partial class RequestLocalizationMiddleware
    {
        [System.ObsoleteAttribute("This constructor is obsolete and will be removed in a future version. Use RequestLocalizationMiddleware(RequestDelegate next, IOptions<RequestLocalizationOptions> options, ILoggerFactory loggerFactory) instead")]
        public RequestLocalizationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions> options) { }
        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute]
        public RequestLocalizationMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.RequestLocalizationOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
}
