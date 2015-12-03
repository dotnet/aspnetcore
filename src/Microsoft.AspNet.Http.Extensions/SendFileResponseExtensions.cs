// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Http
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
        /// <param name="fileName">The full path to the file.</param>
        /// <returns></returns>
        public static Task SendFileAsync(this HttpResponse response, string fileName)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return response.SendFileAsync(fileName, 0, null, CancellationToken.None);
        }

        /// <summary>
        /// Sends the given file using the SendFile extension.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="fileName">The full path to the file.</param>
        /// <param name="offset">The offset in the file.</param>
        /// <param name="count">The number of types to send, or null to send the remainder of the file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SendFileAsync(this HttpResponse response, string fileName, long offset, long? count, CancellationToken cancellationToken)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile == null)
            {
                return SendFileAsync(response.Body, fileName, offset, count, cancellationToken);
            }

            return sendFile.SendFileAsync(fileName, offset, count, cancellationToken);
        }

        // Not safe for overlapped writes.
        private static async Task SendFileAsync(Stream outputStream, string fileName, long offset, long? length, CancellationToken cancel)
        {
            cancel.ThrowIfCancellationRequested();

            var fileInfo = new FileInfo(fileName);
            if (offset < 0 || offset > fileInfo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }

            if (length.HasValue &&
                (length.Value < 0 || length.Value > fileInfo.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, string.Empty);
            }

            int bufferSize = 1024 * 16;

            var fileStream = new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: bufferSize,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (fileStream)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);

                // TODO: Use buffer pool
                var buffer = new byte[bufferSize];

                await StreamCopyOperation.CopyToAsync(fileStream, buffer, outputStream, length, cancel);
            }
        }
    }
}