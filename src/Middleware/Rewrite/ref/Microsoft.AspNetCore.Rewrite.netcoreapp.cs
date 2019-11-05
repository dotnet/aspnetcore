// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class RewriteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRewriter(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseRewriter(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Rewrite
{
    public static partial class ApacheModRewriteOptionsExtensions
    {
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddApacheModRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, Microsoft.Extensions.FileProviders.IFileProvider fileProvider, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddApacheModRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, System.IO.TextReader reader) { throw null; }
    }
    public static partial class IISUrlRewriteOptionsExtensions
    {
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddIISUrlRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, Microsoft.Extensions.FileProviders.IFileProvider fileProvider, string filePath, bool alwaysUseManagedServerVariables = false) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddIISUrlRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, System.IO.TextReader reader, bool alwaysUseManagedServerVariables = false) { throw null; }
    }
    public partial interface IRule
    {
        void ApplyRule(Microsoft.AspNetCore.Rewrite.RewriteContext context);
    }
    public partial class RewriteContext
    {
        public RewriteContext() { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Rewrite.RuleResult Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Extensions.FileProviders.IFileProvider StaticFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class RewriteMiddleware
    {
        public RewriteMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Rewrite.RewriteOptions> options) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class RewriteOptions
    {
        public RewriteOptions() { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Rewrite.IRule> Rules { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.FileProviders.IFileProvider StaticFileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class RewriteOptionsExtensions
    {
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions Add(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, Microsoft.AspNetCore.Rewrite.IRule rule) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions Add(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, System.Action<Microsoft.AspNetCore.Rewrite.RewriteContext> applyRule) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirect(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, string regex, string replacement) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirect(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, string regex, string replacement, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttps(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttps(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttps(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode, int? sslPort) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToHttpsPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToNonWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToNonWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToNonWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode, params string[] domains) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToNonWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, params string[] domains) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToNonWwwPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToNonWwwPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, params string[] domains) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, int statusCode, params string[] domains) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWww(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, params string[] domains) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWwwPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRedirectToWwwPermanent(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, params string[] domains) { throw null; }
        public static Microsoft.AspNetCore.Rewrite.RewriteOptions AddRewrite(this Microsoft.AspNetCore.Rewrite.RewriteOptions options, string regex, string replacement, bool skipRemainingRules) { throw null; }
    }
    public enum RuleResult
    {
        ContinueRules = 0,
        EndResponse = 1,
        SkipRemainingRules = 2,
    }
}
