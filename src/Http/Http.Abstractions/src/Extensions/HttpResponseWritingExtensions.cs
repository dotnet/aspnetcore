// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Convenience methods for writing to the response.
    /// </summary>
    public static class HttpResponseWritingExtensions
    {
        /// <summary>
        /// Writes the given text to the response body. UTF-8 encoding will be used.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponse"/>.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static Task WriteAsync(this HttpResponse response, string text, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return response.WriteAsync(text, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Writes the given text to the response body using the given encoding.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponse"/>.</param>
        /// <param name="text">The text to write to the response.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="cancellationToken">Notifies when request operations should be cancelled.</param>
        /// <returns>A task that represents the completion of the write operation.</returns>
        public static Task WriteAsync(this HttpResponse response, string text, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            byte[] data = encoding.GetBytes(text);
            return response.Body.WriteAsync(data, 0, data.Length, cancellationToken);
        }
    }
}