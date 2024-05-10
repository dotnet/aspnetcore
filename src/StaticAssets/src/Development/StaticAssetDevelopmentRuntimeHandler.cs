// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Builder;

// Handles changes during development to support common scenarios where for example, a developer changes a file in the wwwroot folder.
internal sealed partial class StaticAssetDevelopmentRuntimeHandler(List<StaticAssetDescriptor> descriptors)
{
    public void AttachRuntimePatching(EndpointBuilder builder)
    {
        var original = builder.RequestDelegate!;
        var asset = builder.Metadata.OfType<StaticAssetDescriptor>().Single();
        if (asset.HasContentEncoding())
        {
            // This is a compressed asset, which might get out of "sync" with the original uncompressed version.
            // We are going to find the original by using the weak etag from this compressed asset and locating an asset with the same etag.
            var eTag = asset.GetWeakETag();
            asset = FindOriginalAsset(eTag.Tag.Value!, descriptors);
        }

        builder.RequestDelegate = async context =>
        {
            var originalFeature = context.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
            var fileInfo = context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider.GetFileInfo(asset.AssetFile);
            if (fileInfo.Length != asset.GetContentLength() || fileInfo.LastModified != asset.GetLastModified())
            {
                // At this point, we know that the file has changed from what was generated at build time.
                // This is for example, when someone changes something in the WWWRoot folder.

                // In case we were dealing with a compressed asset, we are going to wrap the response body feature to re-compress the asset on the fly.
                // and write that to the response instead.
                context.Features.Set<IHttpResponseBodyFeature>(new RuntimeStaticAssetResponseBodyFeature(originalFeature, context, asset));
            }

            await original(context);
            context.Features.Set(originalFeature);
        };
    }

    internal static string GetETag(IFileInfo fileInfo)
    {
        using var stream = fileInfo.CreateReadStream();
        return $"\"{Convert.ToBase64String(SHA256.HashData(stream))}\"";
    }

    internal sealed class RuntimeStaticAssetResponseBodyFeature : IHttpResponseBodyFeature
    {
        private readonly IHttpResponseBodyFeature _original;
        private readonly HttpContext _context;
        private readonly StaticAssetDescriptor _asset;

        public RuntimeStaticAssetResponseBodyFeature(IHttpResponseBodyFeature original, HttpContext context, StaticAssetDescriptor asset)
        {
            _original = original;
            _context = context;
            _asset = asset;
        }

        public Stream Stream => _original.Stream;

        public PipeWriter Writer => _original.Writer;

        public Task CompleteAsync()
        {
            return _original.CompleteAsync();
        }

        public void DisableBuffering()
        {
            _original.DisableBuffering();
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            var fileInfo = _context.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider.GetFileInfo(_asset.AssetFile);
            var endpoint = _context.GetEndpoint()!;
            var assetDescriptor = endpoint.Metadata.OfType<StaticAssetDescriptor>().Single();
            _context.Response.Headers.ETag = "";

            if (assetDescriptor.AssetFile != _asset.AssetFile)
            {
                // This was a compressed asset, asset contains the path to the original file, we'll re-compress the asset on the fly and replace the body
                // and the content length.
                using var stream = new MemoryStream();
                using (var fileStream = fileInfo.CreateReadStream())
                {
                    using var gzipStream = new GZipStream(stream, CompressionLevel.NoCompression, leaveOpen: true);
                    fileStream.CopyTo(gzipStream);
                    gzipStream.Flush();
                }
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                _context.Response.Headers.ContentLength = stream.Length;

                var eTag = Convert.ToBase64String(SHA256.HashData(stream));
                var weakETag = $"W/{GetETag(fileInfo)}";

                // Here we add the ETag for the Gzip stream as well as the weak ETag for the original asset.
                _context.Response.Headers.ETag = new StringValues([$"\"{eTag}\"", weakETag]);

                stream.Seek(0, SeekOrigin.Begin);
                return stream.CopyToAsync(_context.Response.Body, cancellationToken);
            }
            else
            {
                // Clear all the ETag headers, as they'll be replaced with the new ones.
                _context.Response.Headers.ETag = "";
                // Compute the new ETag, if this is a compressed asset, HotReloadStaticAsset will update it.
                _context.Response.Headers.ETag = GetETag(fileInfo);
                _context.Response.Headers.ContentLength = fileInfo.Length;
                _context.Response.Headers.LastModified = fileInfo.LastModified.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);

                // Send the modified asset as is.
                return _original.SendFileAsync(fileInfo.PhysicalPath!, 0, fileInfo.Length, cancellationToken);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private static StaticAssetDescriptor FindOriginalAsset(string tag, List<StaticAssetDescriptor> descriptors)
    {
        for (var i = 0; i < descriptors.Count; i++)
        {
            if (descriptors[i].HasETag(tag))
            {
                return descriptors[i];
            }
        }

        throw new InvalidOperationException("The original asset was not found.");
    }

    internal static bool IsEnabled(IServiceProvider serviceProvider, IWebHostEnvironment environment)
    {
        var config = serviceProvider.GetRequiredService<IConfiguration>();
        var explicitlyConfigured = bool.TryParse(config["HotReloadStaticAssets"], out var hotReload);
        return (!explicitlyConfigured && environment.IsDevelopment()) || (explicitlyConfigured && hotReload);
    }

    internal static void EnableSupport(
        IEndpointRouteBuilder endpoints,
        StaticAssetsEndpointConventionBuilder builder,
        IWebHostEnvironment environment,
        List<StaticAssetDescriptor> descriptors)
    {
        var config = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var hotReloadHandler = new StaticAssetDevelopmentRuntimeHandler(descriptors);
        builder.Add(hotReloadHandler.AttachRuntimePatching);
        var disableFallback = bool.TryParse(config["DisableStaticAssetFallback"], out var disableFallbackValue) && disableFallbackValue;

        if (!disableFallback)
        {
            var logger = endpoints.ServiceProvider.GetRequiredService<ILogger<StaticAssetDevelopmentRuntimeHandler>>();

            // Add a fallback static file handler to serve any file that might have been added after the initial startup.
            var fallback = endpoints.MapFallback(
                "{**path:file}",
                endpoints.CreateApplicationBuilder()
                    .Use((ctx, nxt) =>
                    {
                        Log.StaticAssetNotFoundInManifest(logger, ctx.Request.Path);

                        ctx.SetEndpoint(null);
                        ctx.Response.OnStarting((context) =>
                        {
                            var ctx = (HttpContext)context;
                            if (ctx.Response.StatusCode == StatusCodes.Status200OK)
                            {
                                var fileInfo = environment.WebRootFileProvider.GetFileInfo(ctx.Request.Path);
                                if (fileInfo.Exists)
                                {
                                    // Apply the ETag header to the response.
                                    ctx.Response.GetTypedHeaders().ETag = new EntityTagHeaderValue(GetETag(fileInfo));
                                }
                            }
                            return Task.CompletedTask;
                        }, ctx);
                        return nxt();
                    })
                    .UseStaticFiles()
                    .Build());

            // Set up a custom constraint to only match existing files.
            fallback
                .Add(endpoint =>
                {
                    if (endpoint is not RouteEndpointBuilder routeEndpoint || routeEndpoint is not { RoutePattern.RawText: { } pattern })
                    {
                        return;
                    }

                    // Add a custom constraint (not inline) to check if the file exists as part of the route matching
                    routeEndpoint.RoutePattern = RoutePatternFactory.Parse(
                        pattern,
                        null,
                        new RouteValueDictionary { ["path"] = new FileExistsConstraint(environment) });
                });

            // Limit matching to supported methods.
            fallback.Add(b => b.Metadata.Add(new HttpMethodMetadata(["GET", "HEAD"])));
        }
    }

    private static partial class Log
    {
        private const string StaticAssetNotFoundInManifestMessage = """The static asset '{Path}' was not found in the built time manifest. This file will not be available at runtime if it is not available at compile time during the publish process. If the file was not added to the project during development, and is created at runtime, use the StaticFiles middleware to serve it instead.""";

        [LoggerMessage(1, LogLevel.Warning, StaticAssetNotFoundInManifestMessage)]
        public static partial void StaticAssetNotFoundInManifest(ILogger logger, string path);
    }

    private sealed class FileExistsConstraint(IWebHostEnvironment environment) : IRouteConstraint
    {
        private readonly IWebHostEnvironment _environment = environment;

        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values[routeKey] is not string path)
            {
                return false;
            }

            var fileInfo = _environment.WebRootFileProvider.GetFileInfo(path);
            return fileInfo.Exists;
        }
    }
}
