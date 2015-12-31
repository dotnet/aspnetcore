// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="FileResult" /> that on execution writes the file specified using a virtual path to the response
    /// using mechanisms provided by the host.
    /// </summary>
    public class VirtualFileResult : FileResult
    {
        private const int DefaultBufferSize = 0x1000;
        private string _fileName;

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with the provided <paramref name="fileName"/>
        /// and the provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VirtualFileResult(string fileName, string contentType)
            : this(fileName, MediaTypeHeaderValue.Parse(contentType))
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
        }

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VirtualFileResult(string fileName, MediaTypeHeaderValue contentType)
            : base(contentType)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _fileName = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IFileProvider"/> used to resolve paths.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <inheritdoc />
        protected override async Task WriteFileAsync(HttpResponse response)
        {
            var fileProvider = GetFileProvider(response.HttpContext.RequestServices);

            var normalizedPath = FileName;
            if (normalizedPath.StartsWith("~", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Substring(1);
            }

            var fileInfo = fileProvider.GetFileInfo(normalizedPath);
            if (fileInfo.Exists)
            {
                var physicalPath = fileInfo.PhysicalPath;
                var sendFile = response.HttpContext.Features.Get<IHttpSendFileFeature>();
                if (sendFile != null && !string.IsNullOrEmpty(physicalPath))
                {
                    await sendFile.SendFileAsync(
                        physicalPath,
                        offset: 0,
                        length: null,
                        cancellation: default(CancellationToken));
                }
                else
                {
                    var fileStream = GetFileStream(fileInfo);
                    using (fileStream)
                    {
                        await fileStream.CopyToAsync(response.Body, DefaultBufferSize);
                    }
                }
            }
            else
            {
                throw new FileNotFoundException(
                    Resources.FormatFileResult_InvalidPath(FileName), FileName);
            }
        }

        /// <summary>
        /// Returns <see cref="Stream"/> for the specified <paramref name="fileInfo"/>.
        /// </summary>
        /// <param name="fileInfo">The <see cref="IFileInfo"/> for which the stream is needed.</param>
        /// <returns><see cref="Stream"/> for the specified <paramref name="fileInfo"/>.</returns>
        protected virtual Stream GetFileStream(IFileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            return fileInfo.CreateReadStream();
        }

        private IFileProvider GetFileProvider(IServiceProvider requestServices)
        {
            if (FileProvider != null)
            {
                return FileProvider;
            }

            var hostingEnvironment = requestServices.GetService<IHostingEnvironment>();
            FileProvider = hostingEnvironment.WebRootFileProvider;

            return FileProvider;
        }
    }
}
