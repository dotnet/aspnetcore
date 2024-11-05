// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods to read the request body as JSON.
/// </summary>
public static class HttpRequestJsonExtensions
{
    private const string RequiresUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. " +
        "Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";
    private const string RequiresDynamicCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed and need runtime code generation. " +
        "Use the overload that takes a JsonTypeInfo or JsonSerializerContext for native AOT applications.";

    /// <summary>
    /// Read JSON from the request and deserialize to the specified type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <typeparam name="TValue">The type of object to read.</typeparam>
    /// <param name="request">The request to read from.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public static ValueTask<TValue?> ReadFromJsonAsync<TValue>(
        this HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = ResolveSerializerOptions(request.HttpContext);
        return request.ReadFromJsonAsync(jsonTypeInfo: (JsonTypeInfo<TValue>)options.GetTypeInfo(typeof(TValue)), cancellationToken);
    }

    /// <summary>
    /// Read JSON from the request and deserialize to the specified type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <typeparam name="TValue">The type of object to read.</typeparam>
    /// <param name="request">The request to read from.</param>
    /// <param name="options">The serializer options to use when deserializing the content.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask<TValue?> ReadFromJsonAsync<TValue>(
        this HttpRequest request,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HasJsonContentType(out var charset))
        {
            ThrowContentTypeError(request);
        }

        options ??= ResolveSerializerOptions(request.HttpContext);

        var encoding = GetEncodingFromCharset(charset);
        var (inputStream, usesTranscodingStream) = GetInputStream(request.HttpContext, encoding);

        try
        {
            return await JsonSerializer.DeserializeAsync<TValue>(inputStream, options, cancellationToken);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await inputStream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Read JSON from the request and deserialize to the specified type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <param name="request">The request to read from.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The deserialized value.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async ValueTask<TValue?> ReadFromJsonAsync<TValue>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpRequest request,
        JsonTypeInfo<TValue> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HasJsonContentType(out var charset))
        {
            ThrowContentTypeError(request);
        }

        var encoding = GetEncodingFromCharset(charset);
        var (inputStream, usesTranscodingStream) = GetInputStream(request.HttpContext, encoding);

        try
        {
            return await JsonSerializer.DeserializeAsync(inputStream, jsonTypeInfo, cancellationToken);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await inputStream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Read JSON from the request and deserialize to object type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <param name="request">The request to read from.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The deserialized value.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async ValueTask<object?> ReadFromJsonAsync(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpRequest request,
        JsonTypeInfo jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HasJsonContentType(out var charset))
        {
            ThrowContentTypeError(request);
        }

        var encoding = GetEncodingFromCharset(charset);
        var (inputStream, usesTranscodingStream) = GetInputStream(request.HttpContext, encoding);

        try
        {
            return await JsonSerializer.DeserializeAsync(inputStream, jsonTypeInfo, cancellationToken);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await inputStream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Read JSON from the request and deserialize to the specified type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <param name="request">The request to read from.</param>
    /// <param name="type">The type of object to read.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public static ValueTask<object?> ReadFromJsonAsync(
        this HttpRequest request,
        Type type,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = ResolveSerializerOptions(request.HttpContext);
        return request.ReadFromJsonAsync(jsonTypeInfo: options.GetTypeInfo(type), cancellationToken);
    }

    /// <summary>
    /// Read JSON from the request and deserialize to the specified type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <param name="request">The request to read from.</param>
    /// <param name="type">The type of object to read.</param>
    /// <param name="options">The serializer options use when deserializing the content.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask<object?> ReadFromJsonAsync(
        this HttpRequest request,
        Type type,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(type);

        if (!request.HasJsonContentType(out var charset))
        {
            ThrowContentTypeError(request);
        }

        options ??= ResolveSerializerOptions(request.HttpContext);

        var encoding = GetEncodingFromCharset(charset);
        var (inputStream, usesTranscodingStream) = GetInputStream(request.HttpContext, encoding);

        try
        {
            return await JsonSerializer.DeserializeAsync(inputStream, type, options, cancellationToken);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await inputStream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Read JSON from the request and deserialize to the specified type.
    /// If the request's content-type is not a known JSON type then an error will be thrown.
    /// </summary>
    /// <param name="request">The request to read from.</param>
    /// <param name="type">The type of object to read.</param>
    /// <param name="context">A metadata provider for serializable types.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The deserialized value.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static async ValueTask<object?> ReadFromJsonAsync(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpRequest request,
        Type type,
        JsonSerializerContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);

        if (!request.HasJsonContentType(out var charset))
        {
            ThrowContentTypeError(request);
        }

        var encoding = GetEncodingFromCharset(charset);
        var (inputStream, usesTranscodingStream) = GetInputStream(request.HttpContext, encoding);

        try
        {
            return await JsonSerializer.DeserializeAsync(inputStream, type, context, cancellationToken);
        }
        finally
        {
            if (usesTranscodingStream)
            {
                await inputStream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Checks the Content-Type header for JSON types.
    /// </summary>
    /// <returns>true if the Content-Type header represents a JSON content type; otherwise, false.</returns>
    public static bool HasJsonContentType(this HttpRequest request)
    {
        return request.HasJsonContentType(out _);
    }

    private static bool HasJsonContentType(this HttpRequest request, out StringSegment charset)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mt))
        {
            charset = StringSegment.Empty;
            return false;
        }

        // Matches application/json
        if (mt.MediaType.Equals(ContentTypeConstants.JsonContentType, StringComparison.OrdinalIgnoreCase))
        {
            charset = mt.Charset;
            return true;
        }

        // Matches +json, e.g. application/ld+json
        if (mt.Suffix.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            charset = mt.Charset;
            return true;
        }

        charset = StringSegment.Empty;
        return false;
    }

    private static JsonSerializerOptions ResolveSerializerOptions(HttpContext httpContext)
    {
        // Attempt to resolve options from DI then fallback to default options
        return httpContext.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value?.SerializerOptions ?? JsonOptions.DefaultSerializerOptions;
    }

    [DoesNotReturn]
    private static void ThrowContentTypeError(HttpRequest request)
    {
        throw new InvalidOperationException($"Unable to read the request as JSON because the request content type '{request.ContentType}' is not a known JSON content type.");
    }

    private static (Stream inputStream, bool usesTranscodingStream) GetInputStream(HttpContext httpContext, Encoding? encoding)
    {
        if (encoding == null || encoding.CodePage == Encoding.UTF8.CodePage)
        {
            return (httpContext.Request.Body, false);
        }

        var inputStream = Encoding.CreateTranscodingStream(httpContext.Request.Body, encoding, Encoding.UTF8, leaveOpen: true);
        return (inputStream, true);
    }

    private static Encoding? GetEncodingFromCharset(StringSegment charset)
    {
        if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            // This is an optimization for utf-8 that prevents the Substring caused by
            // charset.Value
            return Encoding.UTF8;
        }

        try
        {
            // charset.Value might be an invalid encoding name as in charset=invalid.
            return charset.HasValue ? Encoding.GetEncoding(charset.Value) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to read the request as JSON because the request content type charset '{charset}' is not a known encoding.", ex);
        }
    }
}
