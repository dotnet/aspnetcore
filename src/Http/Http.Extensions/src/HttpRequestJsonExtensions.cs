// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods to read the request body as JSON.
    /// </summary>
    public static class HttpRequestJsonExtensions
    {
        /// <summary>
        /// Read JSON from the request and deserialize to the specified type.
        /// If the request's content-type is not a known JSON type then an error will be thrown.
        /// </summary>
        /// <typeparam name="TValue">The type of object to read.</typeparam>
        /// <param name="request">The request to read from.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static ValueTask<TValue?> ReadFromJsonAsync<TValue>(
            this HttpRequest request,
            CancellationToken cancellationToken = default)
        {
            return request.ReadFromJsonAsync<TValue>(options: null, cancellationToken);
        }

        /// <summary>
        /// Read JSON from the request and deserialize to the specified type.
        /// If the request's content-type is not a known JSON type then an error will be thrown.
        /// </summary>
        /// <typeparam name="TValue">The type of object to read.</typeparam>
        /// <param name="request">The request to read from.</param>
        /// <param name="options">The serializer options use when deserializing the content.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static async ValueTask<TValue?> ReadFromJsonAsync<TValue>(
            this HttpRequest request,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.HasJsonContentType(out var charset))
            {
                throw CreateContentTypeError(request);
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
        /// <param name="type">The type of object to read.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static ValueTask<object?> ReadFromJsonAsync(
            this HttpRequest request,
            Type type,
            CancellationToken cancellationToken = default)
        {
            return request.ReadFromJsonAsync(type, options: null, cancellationToken);
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
        [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
        public static async ValueTask<object?> ReadFromJsonAsync(
            this HttpRequest request,
            Type type,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!request.HasJsonContentType(out var charset))
            {
                throw CreateContentTypeError(request);
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
        /// Checks the Content-Type header for JSON types.
        /// </summary>
        /// <returns>true if the Content-Type header represents a JSON content type; otherwise, false.</returns>
        public static bool HasJsonContentType(this HttpRequest request)
        {
            return request.HasJsonContentType(out _);
        }

        private static bool HasJsonContentType(this HttpRequest request, out StringSegment charset)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mt))
            {
                charset = StringSegment.Empty;
                return false;
            }

            // Matches application/json
            if (mt.MediaType.Equals(JsonConstants.JsonContentType, StringComparison.OrdinalIgnoreCase))
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

        private static InvalidOperationException CreateContentTypeError(HttpRequest request)
        {
            return new InvalidOperationException($"Unable to read the request as JSON because the request content type '{request.ContentType}' is not a known JSON content type.");
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
}
