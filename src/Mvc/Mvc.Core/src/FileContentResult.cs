// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a binary file to the response.
    /// </summary>
    public class FileContentResult : FileResult
    {
        private byte[] _fileContents;

        /// <summary>
        /// Creates a new <see cref="FileContentResult"/> instance with
        /// the provided <paramref name="fileContents"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileContents">The bytes that represent the file contents.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FileContentResult(byte[] fileContents, string contentType)
            : this(fileContents, MediaTypeHeaderValue.Parse(contentType))
        {
        }

        /// <summary>
        /// Creates a new <see cref="FileContentResult"/> instance with
        /// the provided <paramref name="fileContents"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="fileContents">The bytes that represent the file contents.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public FileContentResult(byte[] fileContents, MediaTypeHeaderValue contentType)
            : base(contentType?.ToString())
        {
            if (fileContents == null)
            {
                throw new ArgumentNullException(nameof(fileContents));
            }

            FileContents = fileContents;
        }

        /// <summary>
        /// Gets or sets the file contents.
        /// </summary>
        public byte[] FileContents
        {
            get => _fileContents;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _fileContents = value;
            }
        }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<FileContentResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
