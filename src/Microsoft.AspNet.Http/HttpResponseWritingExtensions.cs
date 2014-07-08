// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http
{
    /// <summary>
    /// Convenience methods for writing to the response.
    /// </summary>
    public static class HttpResponseWritingExtensions
    {
        /// <summary>
        /// Writes the given text to the response body. UTF-8 encoding will be used.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="text"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task WriteAsync([NotNull] this HttpResponse response, [NotNull] string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            return response.WriteAsync(text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Writes the given text to the response body using the given encoding.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task WriteAsync([NotNull] this HttpResponse response, [NotNull] string text, [NotNull] Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            byte[] data = encoding.GetBytes(text);
            return response.Body.WriteAsync(data, 0, data.Length, cancellationToken);
        }
    }
}