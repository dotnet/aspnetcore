// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="FileResult"/> on execution will write a file from disk to the response
    /// using mechanisms provided by the host.
    /// </summary>
    public class PhysicalFileResult : FileResult, IResult
    {
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, string contentType)
            : this(fileName, MediaTypeHeaderValue.Parse(contentType))
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
        }

        /// <summary>
        /// Creates a new <see cref="PhysicalFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public PhysicalFileResult(string fileName, MediaTypeHeaderValue contentType)
            : base(contentType.ToString())
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get => _fileName;
            [MemberNotNull(nameof(_fileName))]
            set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<PhysicalFileResult>>();
            return executor.ExecuteAsync(context, this);
        }

        Task IResult.ExecuteAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var fileInfo = new FileInfo(FileName);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(
                    Resources.FormatFileResult_InvalidPath(FileName), FileName);
            }

            return ExecuteAsyncInternal(httpContext, this, fileInfo.LastWriteTimeUtc, fileInfo.Length);
        }

        internal static Task ExecuteAsyncInternal(
            HttpContext httpContext,
            PhysicalFileResult result,
            DateTimeOffset fileLastModified,
            long fileLength)
        {
            var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<RedirectResult>();

            logger.ExecutingFileResult(result, result.FileName);

            var lastModified = result.LastModified ?? fileLastModified;
            var (range, rangeLength, serveBody) = FileResultExecutorBase.SetHeadersAndLog(
                httpContext,
                result,
                fileLength,
                result.EnableRangeProcessing,
                lastModified,
                result.EntityTag,
                logger);

            if (serveBody)
            {
                return PhysicalFileResultExecutor.WriteFileAsyncInternal(httpContext, result, range, rangeLength, logger);
            }

            return Task.CompletedTask;
        }
    }
}
