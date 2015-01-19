// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.Framework.Logging;

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
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new instance of the SendFileMiddleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="loggerFactory">An <see cref="ILoggerFactory"/> instance used to create loggers.</param>
        public SendFileMiddleware([NotNull] RequestDelegate next, [NotNull] ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.Create<SendFileMiddleware>();
        }

        public Task Invoke(HttpContext context)
        {
            // Check if there is a SendFile feature already present
            if (context.GetFeature<IHttpSendFileFeature>() == null)
            {
                context.SetFeature<IHttpSendFileFeature>(new SendFileWrapper(context.Response.Body, _logger));
            }

            return _next(context);
        }

        private class SendFileWrapper : IHttpSendFileFeature
        {
            private readonly Stream _output;
            private readonly ILogger _logger;

            internal SendFileWrapper(Stream output, ILogger logger)
            {
                _output = output;
                _logger = logger;
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
                    if (_logger.IsEnabled(LogLevel.Verbose))
                    {
                        _logger.WriteVerbose(string.Format("Copying bytes {0}-{1} of file {2} to response body", offset, length != null ? (offset + length).ToString() : "*", fileName));
                    }
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
