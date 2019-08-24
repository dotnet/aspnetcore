// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static partial class SpaRouteExtensions
    {
        public static void MapSpaFallbackRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, object defaults, object constraints = null, object dataTokens = null) { }
        public static void MapSpaFallbackRoute(this Microsoft.AspNetCore.Routing.IRouteBuilder routeBuilder, string name, string templatePrefix, object defaults, object constraints = null, object dataTokens = null) { }
    }
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static partial class WebpackDevMiddleware
    {
        [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        public static void UseWebpackDevMiddleware(this Microsoft.AspNetCore.Builder.IApplicationBuilder appBuilder, Microsoft.AspNetCore.SpaServices.Webpack.WebpackDevMiddlewareOptions options = null) { }
    }
}
namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public partial interface ISpaPrerenderer
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.SpaServices.Prerendering.RenderToStringResult> RenderToString(string moduleName, string exportName = null, object customDataParameter = null, int timeoutMilliseconds = 0);
    }
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public partial class JavaScriptModuleExport
    {
        public JavaScriptModuleExport(string moduleName) { }
        public string ExportName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ModuleName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static partial class Prerenderer
    {
        [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.SpaServices.Prerendering.RenderToStringResult> RenderToString(string applicationBasePath, Microsoft.AspNetCore.NodeServices.INodeServices nodeServices, System.Threading.CancellationToken applicationStoppingToken, Microsoft.AspNetCore.SpaServices.Prerendering.JavaScriptModuleExport bootModule, string requestAbsoluteUrl, string requestPathAndQuery, object customDataParameter, int timeoutMilliseconds, string requestPathBase) { throw null; }
    }
    [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute(Attributes="asp-prerender-module")]
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public partial class PrerenderTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
    {
        public PrerenderTagHelper(System.IServiceProvider serviceProvider) { }
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("asp-prerender-data")]
        public object CustomDataParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("asp-prerender-export")]
        public string ExportName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("asp-prerender-module")]
        public string ModuleName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNameAttribute("asp-prerender-timeout")]
        public int TimeoutMillisecondsParameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContextAttribute]
        [Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeNotBoundAttribute]
        public Microsoft.AspNetCore.Mvc.Rendering.ViewContext ViewContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task ProcessAsync(Microsoft.AspNetCore.Razor.TagHelpers.TagHelperContext context, Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output) { throw null; }
    }
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public partial class RenderToStringResult
    {
        public RenderToStringResult() { }
        public Newtonsoft.Json.Linq.JObject Globals { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Html { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string RedirectUrl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int? StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string CreateGlobalsAssignmentScript() { throw null; }
    }
}
namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public partial class WebpackDevMiddlewareOptions
    {
        public WebpackDevMiddlewareOptions() { }
        public string ConfigFile { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IDictionary<string, string> EnvironmentVariables { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public object EnvParam { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HotModuleReplacement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IDictionary<string, string> HotModuleReplacementClientOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string HotModuleReplacementEndpoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int HotModuleReplacementServerPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ProjectPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ReactHotModuleReplacement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public static partial class PrerenderingServiceCollectionExtensions
    {
        [System.ObsoleteAttribute("Use Microsoft.AspNetCore.SpaServices.Extensions")]
        public static void AddSpaPrerenderer(this Microsoft.Extensions.DependencyInjection.IServiceCollection serviceCollection) { }
    }
}
