// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.StaticFiles;

/// <summary>
/// Enables serving static files for a given request path
/// </summary>
public class StaticFileMiddleware
{
    private readonly StaticFileOptions _options;
    private readonly PathString _matchUrl;
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly IFileProvider _fileProvider;
    private readonly IContentTypeProvider _contentTypeProvider;

    /// <summary>
    /// Creates a new instance of the StaticFileMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="hostingEnv">The <see cref="IWebHostEnvironment"/> used by this middleware.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> instance used to create loggers.</param>
    public StaticFileMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, IOptions<StaticFileOptions> options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(hostingEnv);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _next = next;
        _options = options.Value;
        _contentTypeProvider = _options.ContentTypeProvider ?? new FileExtensionContentTypeProvider();
        _fileProvider = _options.FileProvider ?? Helpers.ResolveFileProvider(hostingEnv);
        _matchUrl = _options.RequestPath;
        _logger = loggerFactory.CreateLogger<StaticFileMiddleware>();

        // See HostingEnvironmentExtensions.Initialize
        if (_fileProvider is NullFileProvider && _fileProvider == hostingEnv.WebRootFileProvider)
        {
            _logger.WebRootPathNotFound(Path.GetFullPath(Path.Combine(hostingEnv.ContentRootPath, hostingEnv.WebRootPath ?? "wwwroot")));
        }
    }

    /// <summary>
    /// Processes a request to determine if it matches a known file, and if so, serves it.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        if (!ValidateNoEndpointDelegate(context))
        {
            _logger.EndpointMatched();
        }
        else if (!ValidateMethod(context))
        {
            _logger.RequestMethodNotSupported(context.Request.Method);
        }
        else if (!ValidatePath(context, _matchUrl, out var subPath))
        {
            _logger.PathMismatch(subPath);
        }
        else if (!LookupContentType(_contentTypeProvider, _options, subPath, out var contentType))
        {
            _logger.FileTypeNotSupported(subPath);
        }
        else
        {
            // If we get here, we can try to serve the file
            return TryServeStaticFile(context, contentType, subPath);
        }

        return _next(context);
    }

    // Return true because we only want to run if there is no endpoint delegate.
    private static bool ValidateNoEndpointDelegate(HttpContext context) => context.GetEndpoint()?.RequestDelegate is null;

    private static bool ValidateMethod(HttpContext context)
    {
        return Helpers.IsGetOrHeadMethod(context.Request.Method);
    }

    internal static bool ValidatePath(HttpContext context, PathString matchUrl, out PathString subPath) => Helpers.TryMatchPath(context, matchUrl, forDirectory: false, out subPath);

    internal static bool LookupContentType(IContentTypeProvider contentTypeProvider, StaticFileOptions options, PathString subPath, out string? contentType)
    {
        if (contentTypeProvider.TryGetContentType(subPath.Value!, out contentType))
        {
            return true;
        }

        if (options.ServeUnknownFileTypes)
        {
            contentType = options.DefaultContentType;
            return true;
        }

        return false;
    }

    private Task TryServeStaticFile(HttpContext context, string? contentType, PathString subPath)
    {
        var fileContext = new StaticFileContext(context, _options, _logger, _fileProvider, contentType, subPath);

        if (!fileContext.LookupFileInfo())
        {
            _logger.FileNotFound(fileContext.SubPath);
        }
        else
        {
            // If we get here, we can try to serve the file
            return fileContext.ServeStaticFile(context, _next);
        }

        return _next(context);
    }
}
