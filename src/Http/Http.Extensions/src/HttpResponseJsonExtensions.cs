// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http
{
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
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static Task WriteAsJsonAsync<TValue>(
            this HttpResponse response,
            TValue value,
            CancellationToken cancellationToken = default)
        {
            return response.WriteAsJsonAsync<TValue>(value, options: null, contentType: null, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body. The response content-type will be set to
        /// <c>application/json; charset=utf-8</c>.
        /// </summary>
        /// <typeparam name="TValue">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="value">The value to write as JSON.</param>
        /// <param name="options">The serializer options use when serializing the value.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static Task WriteAsJsonAsync<TValue>(
            this HttpResponse response,
            TValue value,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            return response.WriteAsJsonAsync<TValue>(value, options, contentType: null, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body. The response content-type will be set to
        /// the specified content-type.
        /// </summary>
        /// <typeparam name="TValue">The type of object to write.</typeparam>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="value">The value to write as JSON.</param>
        /// <param name="options">The serializer options use when serializing the value.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static Task WriteAsJsonAsync<TValue>(
            this HttpResponse response,
            TValue value,
            JsonSerializerOptions? options,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            options ??= ResolveSerializerOptions(response.HttpContext);

            response.ContentType = contentType ?? JsonConstants.JsonContentTypeWithCharset;
            return JsonSerializer.SerializeAsync<TValue>(response.Body, value, options, cancellationToken);
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
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static Task WriteAsJsonAsync(
            this HttpResponse response,
            object? value,
            Type type,
            CancellationToken cancellationToken = default)
        {
            return response.WriteAsJsonAsync(value, type, options: null, contentType: null, cancellationToken);
        }

        /// <summary>
        /// Write the specified value as JSON to the response body. The response content-type will be set to
        /// <c>application/json; charset=utf-8</c>.
        /// </summary>
        /// <param name="response">The response to write JSON to.</param>
        /// <param name="value">The value to write as JSON.</param>
        /// <param name="type">The type of object to write.</param>
        /// <param name="options">The serializer options use when serializing the value.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
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
        /// <param name="options">The serializer options use when serializing the value.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static Task WriteAsJsonAsync(
            this HttpResponse response,
            object? value,
            Type type,
            JsonSerializerOptions? options,
            string? contentType,
            CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            options ??= ResolveSerializerOptions(response.HttpContext);

            response.ContentType = contentType ?? JsonConstants.JsonContentTypeWithCharset;
            return JsonSerializer.SerializeAsync(response.Body, value, type, options, cancellationToken);
        }

        private static JsonSerializerOptions ResolveSerializerOptions(HttpContext httpContext)
        {
            // Attempt to resolve options from DI then fallback to default options
            return httpContext.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value?.SerializerOptions ?? JsonOptions.DefaultSerializerOptions;
        }
    }
}
