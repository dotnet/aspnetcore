// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class DefaultFilesExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDefaultFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDefaultFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.DefaultFilesOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDefaultFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string requestPath) { throw null; }
    }
    public partial class DefaultFilesOptions : Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptionsBase
    {
        public DefaultFilesOptions() : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public DefaultFilesOptions(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions sharedOptions) : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public System.Collections.Generic.IList<string> DefaultFileNames { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class DirectoryBrowserExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDirectoryBrowser(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDirectoryBrowser(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.DirectoryBrowserOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDirectoryBrowser(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string requestPath) { throw null; }
    }
    public partial class DirectoryBrowserOptions : Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptionsBase
    {
        public DirectoryBrowserOptions() : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public DirectoryBrowserOptions(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions sharedOptions) : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public Microsoft.AspNetCore.StaticFiles.IDirectoryFormatter Formatter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class FileServerExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseFileServer(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseFileServer(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.FileServerOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseFileServer(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, bool enableDirectoryBrowsing) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseFileServer(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string requestPath) { throw null; }
    }
    public partial class FileServerOptions : Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptionsBase
    {
        public FileServerOptions() : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public Microsoft.AspNetCore.Builder.DefaultFilesOptions DefaultFilesOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Builder.DirectoryBrowserOptions DirectoryBrowserOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool EnableDefaultFiles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool EnableDirectoryBrowsing { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Builder.StaticFileOptions StaticFileOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class StaticFileExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStaticFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStaticFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.StaticFileOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseStaticFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string requestPath) { throw null; }
    }
    public partial class StaticFileOptions : Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptionsBase
    {
        public StaticFileOptions() : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public StaticFileOptions(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions sharedOptions) : base (default(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions)) { }
        public Microsoft.AspNetCore.StaticFiles.IContentTypeProvider ContentTypeProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string DefaultContentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.Features.HttpsCompressionMode HttpsCompression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Action<Microsoft.AspNetCore.StaticFiles.StaticFileResponseContext> OnPrepareResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ServeUnknownFileTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class StaticFilesEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToFile(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToFile(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string filePath, Microsoft.AspNetCore.Builder.StaticFileOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToFile(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToFile(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string filePath, Microsoft.AspNetCore.Builder.StaticFileOptions options) { throw null; }
    }
}
namespace Microsoft.AspNetCore.StaticFiles
{
    public partial class DefaultFilesMiddleware
    {
        public DefaultFilesMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnv, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.DefaultFilesOptions> options) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class DirectoryBrowserMiddleware
    {
        public DirectoryBrowserMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnv, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.DirectoryBrowserOptions> options) { }
        public DirectoryBrowserMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnv, System.Text.Encodings.Web.HtmlEncoder encoder, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.DirectoryBrowserOptions> options) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class FileExtensionContentTypeProvider : Microsoft.AspNetCore.StaticFiles.IContentTypeProvider
    {
        public FileExtensionContentTypeProvider() { }
        public FileExtensionContentTypeProvider(System.Collections.Generic.IDictionary<string, string> mapping) { }
        public System.Collections.Generic.IDictionary<string, string> Mappings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool TryGetContentType(string subpath, out string contentType) { throw null; }
    }
    public partial class HtmlDirectoryFormatter : Microsoft.AspNetCore.StaticFiles.IDirectoryFormatter
    {
        public HtmlDirectoryFormatter(System.Text.Encodings.Web.HtmlEncoder encoder) { }
        public virtual System.Threading.Tasks.Task GenerateContentAsync(Microsoft.AspNetCore.Http.HttpContext context, System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileInfo> contents) { throw null; }
    }
    public partial interface IContentTypeProvider
    {
        bool TryGetContentType(string subpath, out string contentType);
    }
    public partial interface IDirectoryFormatter
    {
        System.Threading.Tasks.Task GenerateContentAsync(Microsoft.AspNetCore.Http.HttpContext context, System.Collections.Generic.IEnumerable<Microsoft.Extensions.FileProviders.IFileInfo> contents);
    }
    public partial class StaticFileMiddleware
    {
        public StaticFileMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnv, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Builder.StaticFileOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    public partial class StaticFileResponseContext
    {
        [System.ObsoleteAttribute("Use the constructor that passes in the HttpContext and IFileInfo parameters: StaticFileResponseContext(HttpContext context, IFileInfo file)", false)]
        public StaticFileResponseContext() { }
        public StaticFileResponseContext(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.Extensions.FileProviders.IFileInfo file) { }
        public Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.Extensions.FileProviders.IFileInfo File { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.AspNetCore.StaticFiles.Infrastructure
{
    public partial class SharedOptions
    {
        public SharedOptions() { }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool RedirectToAppendTrailingSlash { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.PathString RequestPath { get { throw null; } set { } }
    }
    public abstract partial class SharedOptionsBase
    {
        protected SharedOptionsBase(Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions sharedOptions) { }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { get { throw null; } set { } }
        public bool RedirectToAppendTrailingSlash { get { throw null; } set { } }
        public Microsoft.AspNetCore.Http.PathString RequestPath { get { throw null; } set { } }
        protected Microsoft.AspNetCore.StaticFiles.Infrastructure.SharedOptions SharedOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DirectoryBrowserServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDirectoryBrowser(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
