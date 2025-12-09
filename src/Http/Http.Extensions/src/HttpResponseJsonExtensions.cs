// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides extension methods for writing a JSON serialized value to the HTTP response.
/// </summary>
public static partial class HttpResponseJsonExtensions
{
    private const string RequiresUnreferencedCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed. " +
        "Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.";
    private const string RequiresDynamicCodeMessage = "JSON serialization and deserialization might require types that cannot be statically analyzed and need runtime code generation. " +
        "Use the overload that takes a JsonTypeInfo or JsonSerializerContext for native AOT applications.";

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// <c>application/json; charset=utf-8</c>.
    /// </summary>
    /// <typeparam name="TValue">The type of object to write.</typeparam>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static Task WriteAsJsonAsync<TValue>(
        this HttpResponse response,
        TValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.WriteAsJsonAsync(value, options: null, contentType: null, cancellationToken);
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// <c>application/json; charset=utf-8</c>.
    /// </summary>
    /// <typeparam name="TValue">The type of object to write.</typeparam>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="options">The serializer options to use when serializing the value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static Task WriteAsJsonAsync<TValue>(
        this HttpResponse response,
        TValue value,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        return response.WriteAsJsonAsync(value, options, contentType: null, cancellationToken);
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// the specified content-type.
    /// </summary>
    /// <typeparam name="TValue">The type of object to write.</typeparam>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="options">The serializer options to use when serializing the value.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static Task WriteAsJsonAsync<TValue>(
        this HttpResponse response,
        TValue value,
        JsonSerializerOptions? options,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        options ??= ResolveSerializerOptions(response.HttpContext);

        response.ContentType = contentType ?? ContentTypeConstants.JsonContentTypeWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsJsonAsyncSlow(response.BodyWriter, value, options, response.HttpContext.RequestAborted);
        }

        return JsonSerializer.SerializeAsync(response.BodyWriter, value, options, cancellationToken);
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// the specified content-type.
    /// </summary>
    /// <typeparam name="TValue">The type of object to write.</typeparam>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public static Task WriteAsJsonAsync<TValue>(
        this HttpResponse response,
        TValue value,
        JsonTypeInfo<TValue> jsonTypeInfo,
        string? contentType = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.ContentType = contentType ?? ContentTypeConstants.JsonContentTypeWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsJsonAsyncSlow(response, value, jsonTypeInfo, response.HttpContext.RequestAborted);
        }

        return JsonSerializer.SerializeAsync(response.BodyWriter, value, jsonTypeInfo, cancellationToken);

        static async Task WriteAsJsonAsyncSlow(HttpResponse response, TValue value, JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                await JsonSerializer.SerializeAsync(response.BodyWriter, value, jsonTypeInfo, cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// the specified content-type.
    /// </summary>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static Task WriteAsJsonAsync(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpResponse response,
        object? value,
        JsonTypeInfo jsonTypeInfo,
        string? contentType = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.ContentType = contentType ?? ContentTypeConstants.JsonContentTypeWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsJsonAsyncSlow(response, value, jsonTypeInfo, response.HttpContext.RequestAborted);
        }

        return JsonSerializer.SerializeAsync(response.BodyWriter, value, jsonTypeInfo, cancellationToken);

        static async Task WriteAsJsonAsyncSlow(HttpResponse response, object? value, JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            try
            {
                await JsonSerializer.SerializeAsync(response.BodyWriter, value, jsonTypeInfo, cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static async Task WriteAsJsonAsyncSlow<TValue>(
        PipeWriter body,
        TValue value,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            await JsonSerializer.SerializeAsync(body, value, options, cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// <c>application/json; charset=utf-8</c>.
    /// </summary>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="type">The type of object to write.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static Task WriteAsJsonAsync(
        this HttpResponse response,
        object? value,
        Type type,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.WriteAsJsonAsync(value, type, options: null,  contentType: null, cancellationToken);
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// <c>application/json; charset=utf-8</c>.
    /// </summary>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="type">The type of object to write.</param>
    /// <param name="options">The serializer options to use when serializing the value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static Task WriteAsJsonAsync(
        this HttpResponse response,
        object? value,
        Type type,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        return response.WriteAsJsonAsync(value, type, options, contentType: null, cancellationToken);
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// the specified content-type.
    /// </summary>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="type">The type of object to write.</param>
    /// <param name="options">The serializer options to use when serializing the value.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static Task WriteAsJsonAsync(
        this HttpResponse response,
        object? value,
        Type type,
        JsonSerializerOptions? options,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(type);

        options ??= ResolveSerializerOptions(response.HttpContext);

        response.ContentType = contentType ?? ContentTypeConstants.JsonContentTypeWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsJsonAsyncSlow(response.BodyWriter, value, type, options,
                response.HttpContext.RequestAborted);
        }

        return JsonSerializer.SerializeAsync(response.BodyWriter, value, type, options, cancellationToken);
    }

    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    private static async Task WriteAsJsonAsyncSlow(
        PipeWriter body,
        object? value,
        Type type,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            await JsonSerializer.SerializeAsync(body, value, type, options, cancellationToken);
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Write the specified value as JSON to the response body. The response content-type will be set to
    /// the specified content-type.
    /// </summary>
    /// <param name="response">The response to write JSON to.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="type">The type of object to write.</param>
    /// <param name="context">A metadata provider for serializable types.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public static Task WriteAsJsonAsync(
        this HttpResponse response,
        object? value,
        Type type,
        JsonSerializerContext context,
        string? contentType = default,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);

        response.ContentType = contentType ?? ContentTypeConstants.JsonContentTypeWithCharset;

        // if no user provided token, pass the RequestAborted token and ignore OperationCanceledException
        if (!cancellationToken.CanBeCanceled)
        {
            return WriteAsJsonAsyncSlow(response.BodyWriter, value, type, context, response.HttpContext.RequestAborted);
        }

        return JsonSerializer.SerializeAsync(response.BodyWriter, value, type, context, cancellationToken);

        static async Task WriteAsJsonAsyncSlow(PipeWriter body, object? value, Type type, JsonSerializerContext context,
            CancellationToken cancellationToken)
        {
            try
            {
                await JsonSerializer.SerializeAsync(body, value, type, context, cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
    }

    private static JsonSerializerOptions ResolveSerializerOptions(HttpContext httpContext)
    {
        // Attempt to resolve options from DI then fallback to default options
        return httpContext.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value?.SerializerOptions ?? JsonOptions.DefaultSerializerOptions;
    }
}
