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
        /// <param name="destination"></param>
        /// <param name="filePath"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public static async Task SendFileAsync(Stream destination, string filePath, long offset, long? count, CancellationToken cancellation)
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

            cancellation.ThrowIfCancellationRequested();

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
                await StreamCopyOperation.CopyToAsync(fileStream, destination, count, bufferSize, cancellation);
            }
        }
    }
}
