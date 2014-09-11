// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// This middleware provides an efficient fallback mechanism for sending static files
    /// when the server does not natively support such a feature.
    /// The caller is responsible for setting all headers in advance.
    /// The caller is responsible for performing the correct impersonation to give access to the file.
    /// </summary>
    public class SendFileMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates a new instance of the SendFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public SendFileMiddleware([NotNull] RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            // Check if there is a SendFile feature already present
            if (context.GetFeature<IHttpSendFileFeature>() == null)
            {
                context.SetFeature<IHttpSendFileFeature>(new SendFileWrapper(context.Response.Body));
            }

            return _next(context);
        }

        private class SendFileWrapper : IHttpSendFileFeature
        {
            private readonly Stream _output;

            internal SendFileWrapper(Stream output)
            {
                _output = output;
            }

            // Not safe for overlapped writes.
            public async Task SendFileAsync(string fileName, long offset, long? length, CancellationToken cancel)
            {
                cancel.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentNullException("fileName");
                }
                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException(string.Empty, fileName);
                }

                var fileInfo = new FileInfo(fileName);
                if (offset < 0 || offset > fileInfo.Length)
                {
                    throw new ArgumentOutOfRangeException("offset", offset, string.Empty);
                }

                if (length.HasValue &&
                    (length.Value < 0 || length.Value > fileInfo.Length - offset))
                {
                    throw new ArgumentOutOfRangeException("length", length, string.Empty);
                }

#if ASPNET50
                Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024 * 64,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
#else
                // TODO: Bring back async when the contract gets it
                Stream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024 * 64);
#endif
                try
                {
                    fileStream.Seek(offset, SeekOrigin.Begin);
                    await StreamCopyOperation.CopyToAsync(fileStream, _output, length, cancel);
                }
                finally
                {
                    fileStream.Dispose();
                }
            }
        }
    }
}
