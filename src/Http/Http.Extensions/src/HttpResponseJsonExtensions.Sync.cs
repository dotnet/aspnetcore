// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides extension methods for writing a JSON serialized value to the HTTP response.
/// </summary>
public static partial class HttpResponseJsonExtensions
{
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
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void WriteAsJson<TValue>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpResponse response,
        TValue value,
        CancellationToken cancellationToken = default)
    {
        if (!Thread.IsGreenThread)
        {
            throw new NotSupportedException();
        }
        response.WriteAsJsonAsync(value, cancellationToken).GetAwaiter().GetResult();
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
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void WriteAsJson<TValue>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpResponse response,
        TValue value,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (!Thread.IsGreenThread)
        {
            throw new NotSupportedException();
        }
        response.WriteAsJsonAsync(value, options, cancellationToken).GetAwaiter().GetResult();
        
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
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void WriteAsJson<TValue>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpResponse response,
        TValue value,
        JsonSerializerOptions? options,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        if (!Thread.IsGreenThread)
        {
            throw new NotSupportedException();
        }
        response.WriteAsJsonAsync(value, options, contentType, cancellationToken).GetAwaiter().GetResult();
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
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static void WriteAsJson<TValue>(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        this HttpResponse response,
        TValue value,
        JsonTypeInfo<TValue> jsonTypeInfo,
        string? contentType = default,
        CancellationToken cancellationToken = default)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (!Thread.IsGreenThread)
        {
            throw new NotSupportedException();
        }
        response.WriteAsJsonAsync(value,jsonTypeInfo, contentType, cancellationToken).GetAwaiter().GetResult();
    }
}
