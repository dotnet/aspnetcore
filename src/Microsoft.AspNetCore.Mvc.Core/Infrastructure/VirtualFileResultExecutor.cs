// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class VirtualFileResultExecutor : FileResultExecutorBase, IActionResultExecutor<VirtualFileResult>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public VirtualFileResultExecutor(ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment)
            : base(CreateLogger<VirtualFileResultExecutor>(loggerFactory))
        {
            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            _hostingEnvironment = hostingEnvironment;
        }

        /// <inheritdoc />
        public virtual Task ExecuteAsync(ActionContext context, VirtualFileResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var fileInfo = GetFileInformation(result);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException(
                    Resources.FormatFileResult_InvalidPath(result.FileName), result.FileName);
            }

            var lastModified = result.LastModified ?? fileInfo.LastModified;
            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                fileInfo.Length,
                result.EnableRangeProcessing,
                lastModified,
                result.EntityTag);

            if (serveBody)
            {
                return WriteFileAsync(context, result, fileInfo, range, rangeLength);
            }

            return Task.CompletedTask;
        }

        protected virtual Task WriteFileAsync(ActionContext context, VirtualFileResult result, IFileInfo fileInfo, RangeItemHeaderValue range, long rangeLength)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (range != null && rangeLength == 0)
            {
                return Task.CompletedTask;
            }

            var response = context.HttpContext.Response;
            var physicalPath = fileInfo.PhysicalPath;
            var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
            {
                if (range != null)
                {
                    return sendFile.SendFileAsync(
                        physicalPath,
                        offset: range.From ?? 0L,
                        count: rangeLength,
                        cancellation: default(CancellationToken));
                }

                return sendFile.SendFileAsync(
                    physicalPath,
                    offset: 0,
                    count: null,
                    cancellation: default(CancellationToken));
            }

            return WriteFileAsync(context.HttpContext, GetFileStream(fileInfo), range, rangeLength);
        }

        private IFileInfo GetFileInformation(VirtualFileResult result)
        {
            var fileProvider = GetFileProvider(result);

            var normalizedPath = result.FileName;
            if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Substring(1);
            }

            var fileInfo = fileProvider.GetFileInfo(normalizedPath);
            return fileInfo;
        }

        private IFileProvider GetFileProvider(VirtualFileResult result)
        {
            if (result.FileProvider != null)
            {
                return result.FileProvider;
            }

            result.FileProvider = _hostingEnvironment.WebRootFileProvider;
            return result.FileProvider;
        }

        protected virtual Stream GetFileStream(IFileInfo fileInfo)
        {
            return fileInfo.CreateReadStream();
        }
    }
}
