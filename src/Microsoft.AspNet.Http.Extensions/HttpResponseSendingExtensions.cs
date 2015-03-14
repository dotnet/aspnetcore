// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Convenience methods for writing to the response.
    /// </summary>
    public static class HttpResponseSendingExtensions
    {
        /// <summary>
        /// Sends a response with the given Content-Type and body. UTF-8 encoding will be used, and the Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="text"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] string text, [NotNull] string contentType, CancellationToken cancellationToken = default(CancellationToken))
        {
            return response.SendAsync(text, Encoding.UTF8, contentType, cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given Content-Type, encoding, and body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] string text, [NotNull] Encoding encoding, [NotNull] string contentType, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentException("Empty Content-Type is not allowed.");
            }
            if (contentType.IndexOf("charset=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                contentType += "; charset=" + encoding.WebName;
            }
            response.ContentType = contentType;
            return response.SendAsync(text, encoding, cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given body. UTF-8 encoding will be used, and the Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="text"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            return response.SendAsync(text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given encoding and body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] string text, [NotNull] Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            byte[] data = encoding.GetBytes(text);
            return response.SendAsync(data, cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given Content-Type and body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] byte[] data, [NotNull] string contentType, CancellationToken cancellationToken = default(CancellationToken))
        {
            return response.SendAsync(new ArraySegment<byte>(data), contentType, cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given Content-Type and body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="data"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, ArraySegment<byte> data, [NotNull] string contentType, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentException("Empty Content-Type is not allowed.");
            }
            response.ContentType = contentType;
            return response.SendAsync(data, cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] byte[] data, CancellationToken cancellationToken = default(CancellationToken))
        {
            return response.SendAsync(new ArraySegment<byte>(data), cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, [NotNull] byte[] data, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
        {
            return response.SendAsync(new ArraySegment<byte>(data, offset, count), cancellationToken);
        }

        /// <summary>
        /// Sends a response with the given body. The Content-Length header will be set accordingly.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendAsync([NotNull] this HttpResponse response, ArraySegment<byte> data, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (data.Array == null)
            {
                throw new ArgumentException("The Array cannot be null.", "data"); // TODO: LOC
            }
            response.ContentLength = data.Count;
            return response.Body.WriteAsync(data.Array, data.Offset, data.Count, cancellationToken);
        }
    }
}