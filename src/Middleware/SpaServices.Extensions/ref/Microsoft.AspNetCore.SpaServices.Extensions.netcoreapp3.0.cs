// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class SpaApplicationBuilderExtensions
    {
        public static void UseSpa(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, System.Action<Microsoft.AspNetCore.SpaServices.ISpaBuilder> configuration) { }
    }
    [System.ObsoleteAttribute("Prerendering is no longer supported out of box")]
    public static partial class SpaPrerenderingExtensions
    {
        [System.ObsoleteAttribute("Prerendering is no longer supported out of box")]
        public static void UseSpaPrerendering(this Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder, System.Action<Microsoft.AspNetCore.Builder.SpaPrerenderingOptions> configuration) { }
    }
    [System.ObsoleteAttribute("Prerendering is no longer supported out of box")]
    public partial class SpaPrerenderingOptions
    {
        public SpaPrerenderingOptions() { }
        public Microsoft.AspNetCore.SpaServices.Prerendering.ISpaPrerendererBuilder BootModuleBuilder { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string BootModulePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string[] ExcludeUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Action<Microsoft.AspNetCore.Http.HttpContext, System.Collections.Generic.IDictionary<string, object>> SupplyData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class SpaProxyingExtensions
    {
        public static void UseProxyToSpaDevelopmentServer(this Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder, System.Func<System.Threading.Tasks.Task<System.Uri>> baseUriTaskFactory) { }
        public static void UseProxyToSpaDevelopmentServer(this Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder, string baseUri) { }
        public static void UseProxyToSpaDevelopmentServer(this Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder, System.Uri baseUri) { }
    }
}
namespace Microsoft.AspNetCore.SpaServices
{
    public partial interface ISpaBuilder
    {
        Microsoft.AspNetCore.Builder.IApplicationBuilder ApplicationBuilder { get; }
        Microsoft.AspNetCore.SpaServices.SpaOptions Options { get; }
    }
    public partial class SpaOptions
    {
        public SpaOptions() { }
        public Microsoft.AspNetCore.Http.PathString DefaultPage { get { throw null; } set { } }
        public Microsoft.AspNetCore.Builder.StaticFileOptions DefaultPageStaticFileOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string SourcePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan StartupTimeout { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    [System.ObsoleteAttribute("Prerendering is no longer supported out of box")]
    public partial class AngularCliBuilder : Microsoft.AspNetCore.SpaServices.Prerendering.ISpaPrerendererBuilder
    {
        public AngularCliBuilder(string npmScript) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Build(Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder) { throw null; }
    }
    public static partial class AngularCliMiddlewareExtensions
    {
        public static void UseAngularCliServer(this Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder, string npmScript) { }
    }
}
namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    [System.ObsoleteAttribute("Prerendering is no longer supported out of box")]
    public partial interface ISpaPrerendererBuilder
    {
        System.Threading.Tasks.Task Build(Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder);
    }
}
namespace Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
{
    public static partial class ReactDevelopmentServerMiddlewareExtensions
    {
        public static void UseReactDevelopmentServer(this Microsoft.AspNetCore.SpaServices.ISpaBuilder spaBuilder, string npmScript) { }
    }
}
namespace Microsoft.AspNetCore.SpaServices.StaticFiles
{
    public partial interface ISpaStaticFileProvider
    {
        Microsoft.Extensions.FileProviders.IFileProvider FileProvider { get; }
    }
    public partial class SpaStaticFilesOptions
    {
        public SpaStaticFilesOptions() { }
        public string RootPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SpaStaticFilesExtensions
    {
        public static void AddSpaStaticFiles(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.SpaServices.StaticFiles.SpaStaticFilesOptions> configuration = null) { }
        public static void UseSpaStaticFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder applicationBuilder) { }
        public static void UseSpaStaticFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder applicationBuilder, Microsoft.AspNetCore.Builder.StaticFileOptions options) { }
    }
}
