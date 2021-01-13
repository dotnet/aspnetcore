// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    public static class SendFileFallback
    {
        /// <summary>
        /// Copies the segment of the file to the destination stream.
        /// </summary>
        /// <param name="destination">The stream to write the file segment to.</param>
        /// <param name="filePath">The full disk path to the file.</param>
        /// <param name="offset">The offset in the file to start at.</param>
        /// <param name="count">The number of bytes to send, or null to send the remainder of the file.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to abort the transmission.</param>
        /// <returns></returns>
        public static async Task SendFileAsync(Stream destination, string filePath, long offset, long? count, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(filePath);
            if (offset < 0 || offset > fileInfo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }
            if (count.HasValue &&
                (count.Value < 0 || count.Value > fileInfo.Length - offset))
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, string.Empty);
            }

            cancellationToken.ThrowIfCancellationRequested();

            int bufferSize = 1024 * 16;

            var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: bufferSize,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            using (fileStream)
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                await StreamCopyOperationInternal.CopyToAsync(fileStream, destination, count, bufferSize, cancellationToken);
            }
        }
    }
}
