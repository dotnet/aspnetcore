// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server;

/// <summary>
/// Middleware that handles secure image registration and delivery for Blazor components.
/// </summary>
internal sealed class ImageEndpointMiddleware
{
    private const string RegisterPath = "/_blazor/image/register";
    private const string GetImagePathPrefix = "/_blazor/image/get/";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _protectorPurpose = "BlazorImageComponent";
    private readonly TimeSpan _timeToLive;

    private readonly RequestDelegate _next;
    private readonly ILogger<ImageEndpointMiddleware> _logger;
    private readonly IDataProtectionProvider _dataProtection;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of <see cref="ImageEndpointMiddleware"/>.
    /// </summary>
    public ImageEndpointMiddleware(
        RequestDelegate next,
        ILogger<ImageEndpointMiddleware> logger,
        IDataProtectionProvider dataProtection,
        IMemoryCache cache,
        TimeSpan? timeToLive = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeToLive = timeToLive ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Process an HTTP request.
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value;

        if (path is null)
        {
            await _next(context);
            return;
        }

        if (path.Equals(RegisterPath, StringComparison.OrdinalIgnoreCase) && HttpMethods.IsPost(request.Method))
        {
            await HandleImageRegistrationAsync(context);
            return;
        }

        if (path.StartsWith(GetImagePathPrefix, StringComparison.OrdinalIgnoreCase) && HttpMethods.IsGet(request.Method))
        {
            await HandleImageRetrievalAsync(context);
            return;
        }

        await _next(context);
    }

    private async Task HandleImageRegistrationAsync(HttpContext context)
    {
        if (!context.Request.HasJsonContentType())
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            return;
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<ImageRegistrationRequest>(
                context.Request.Body,
                _jsonOptions,
                context.RequestAborted);

            if (request is null || request.ImageData is null || request.ImageData.Length == 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await WriteJsonErrorAsync(context, "Image data is required");
                return;
            }

            var token = Guid.NewGuid().ToString("N");
            var imageInfo = new ImageInfo
            {
                Data = request.ImageData,
                ContentType = request.ContentType ?? MediaTypeNames.Application.Octet
            };

            _cache.Set(token, imageInfo, _timeToLive);

            // Create protected token
            var protector = _dataProtection.CreateProtector(_protectorPurpose);
            var protectedToken = protector.Protect(token);

            var imageUrl = $"{GetImagePathPrefix}{protectedToken}";

            if (!string.IsNullOrEmpty(context.Request.Scheme) && context.Request.Host.HasValue)
            {
                imageUrl = $"{context.Request.Scheme}://{context.Request.Host}{imageUrl}";
            }

            context.Response.ContentType = MediaTypeNames.Application.Json;
            await JsonSerializer.SerializeAsync(context.Response.Body, new { Url = imageUrl });

            Log.ImageRegistered(_logger, token);
        }
        catch (Exception ex)
        {
            Log.ImageRegistrationFailed(_logger, ex);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteJsonErrorAsync(context, "Failed to process image registration");
        }
    }

    private async Task HandleImageRetrievalAsync(HttpContext context)
    {
        string token = context.Request.Path.Value!.Substring(GetImagePathPrefix.Length);

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "Token is required");
            return;
        }

        try
        {
            // Unprotect the token
            var protector = _dataProtection.CreateProtector(_protectorPurpose);
            var cacheKey = protector.Unprotect(token);

            if (_cache.TryGetValue(cacheKey, out ImageInfo imageInfo))
            {
                context.Response.ContentType = imageInfo.ContentType;
                await context.Response.Body.WriteAsync(imageInfo.Data, context.RequestAborted);

                Log.ImageRetrieved(_logger, cacheKey);
                return;
            }

            Log.ImageNotFound(_logger, cacheKey);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonErrorAsync(context, "Image not found or expired");
        }
        catch (Exception ex)
        {
            Log.ImageRetrievalFailed(_logger, ex);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonErrorAsync(context, "Invalid token");
        }
    }

    private static async Task WriteJsonErrorAsync(HttpContext context, string message)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await JsonSerializer.SerializeAsync(context.Response.Body, new { Error = message });
    }

    private static class Log
    {
        private static readonly Action<ILogger, string, Exception?> _imageRegistered =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "ImageRegistered"),
                "Image registered with token '{Token}'");

        private static readonly Action<ILogger, Exception> _imageRegistrationFailed =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(2, "ImageRegistrationFailed"),
                "Image registration failed");

        private static readonly Action<ILogger, string, Exception?> _imageRetrieved =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "ImageRetrieved"),
                "Image retrieved with cache key '{CacheKey}'");

        private static readonly Action<ILogger, string, Exception?> _imageNotFound =
            LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(4, "ImageNotFound"),
                "Image not found for cache key '{CacheKey}'");

        private static readonly Action<ILogger, Exception> _imageRetrievalFailed =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(5, "ImageRetrievalFailed"),
                "Image retrieval failed");

        public static void ImageRegistered(ILogger logger, string token) =>
            _imageRegistered(logger, token, null);

        public static void ImageRegistrationFailed(ILogger logger, Exception ex) =>
            _imageRegistrationFailed(logger, ex);

        public static void ImageRetrieved(ILogger logger, string cacheKey) =>
            _imageRetrieved(logger, cacheKey, null);

        public static void ImageNotFound(ILogger logger, string cacheKey) =>
            _imageNotFound(logger, cacheKey, null);

        public static void ImageRetrievalFailed(ILogger logger, Exception ex) =>
            _imageRetrievalFailed(logger, ex);
    }

    /// <summary>
    /// Model for image registration request.
    /// </summary>
    private class ImageRegistrationRequest
    {
        public byte[]? ImageData { get; set; }
        public string? ContentType { get; set; }
    }

    /// <summary>
    /// Internal storage model for image data.
    /// </summary>
    private class ImageInfo
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = MediaTypeNames.Application.Octet;
    }
}

/// <summary>
/// Extension methods for configuring image endpoint middleware in the application pipeline.
/// </summary>
public static class ImageEndpointMiddlewareExtensions
{
    /// <summary>
    /// Adds the image endpoint middleware to the application pipeline.
    /// This middleware handles secure image registration and delivery for Blazor components.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseImageEndpoint(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ImageEndpointMiddleware>();
    }
}
