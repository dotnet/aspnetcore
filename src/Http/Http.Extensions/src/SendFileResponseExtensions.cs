// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides extensions for HttpResponse exposing the SendFile extension.
    /// </summary>
    public static class SendFileResponseExtensions
    {
        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="file">The file.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public static Task SendFileAsync(this HttpResponse response, IFileInfo file, CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return SendFileAsyncCore(response, file, 0, null, cancellationToken);
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="file">The file.</param>
        /// <param name="offset">The offset in the file.</param>
        /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendFileAsync(this HttpResponse response, IFileInfo file, long offset, long? count, CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return SendFileAsyncCore(response, file, offset, count, cancellationToken);
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName">The full path to the file.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public static Task SendFileAsync(this HttpResponse response, string fileName, CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return SendFileAsyncCore(response, fileName, 0, null, cancellationToken);
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName">The full path to the file.</param>
        /// <param name="offset">The offset in the file.</param>
        /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendFileAsync(this HttpResponse response, string fileName, long offset, long? count, CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return SendFileAsyncCore(response, fileName, offset, count, cancellationToken);
        }

        private static async Task SendFileAsyncCore(HttpResponse response, IFileInfo file, long offset, long? count, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(file.PhysicalPath))
            {
                CheckRange(offset, count, file.Length);

                using (var fileContent = file.CreateReadStream())
                {
                    if (offset > 0)
                    {
                        fileContent.Seek(offset, SeekOrigin.Begin);
                    }
                    await StreamCopyOperation.CopyToAsync(fileContent, response.Body, count, cancellationToken);
                }
            }
            else
            {
                await response.SendFileAsync(file.PhysicalPath, offset, count, cancellationToken);
            }
        }

        private static Task SendFileAsyncCore(HttpResponse response, string fileName, long offset, long? count, CancellationToken cancellationToken = default)
        {
            var sendFile = response.HttpContext.Features.Get<IHttpResponseBodyFeature>();
            return sendFile.SendFileAsync(fileName, offset, count, cancellationToken);
        }

        private static void CheckRange(long offset, long? count, long fileLength)
        {
            if (offset < 0 || offset > fileLength)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }
            if (count.HasValue &&
                (count.GetValueOrDefault() < 0 || count.GetValueOrDefault() > fileLength - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
            }
        }
    }
}
