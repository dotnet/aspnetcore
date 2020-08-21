// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="FileResult" /> that on execution writes the file specified using a virtual path to the response
    /// using mechanisms provided by the host.
    /// </summary>
    public class VirtualFileResult : FileResult
    {
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
        }

        /// <summary>
        /// Creates a new <see cref="VirtualFileResult"/> instance with
        /// the provided <paramref name="fileName"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileName">The path to the file. The path must be relative/virtual.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public VirtualFileResult(string fileName, MediaTypeHeaderValue contentType)
            : base(contentType?.ToString())
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        /// <summary>
        /// Gets or sets the path to the file that will be sent back as the response.
        /// </summary>
        public string FileName
        {
            get => _fileName;
            set => _fileName = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets the <see cref="IFileProvider"/> used to resolve paths.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<VirtualFileResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
