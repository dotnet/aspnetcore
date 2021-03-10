// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles
{
    /// <summary>
    /// Contains information about the request and the file that will be served in response.
    /// </summary>
    public class StaticFileResponseContext
    {
        /// <summary>
        /// Constructs the <see cref="StaticFileResponseContext"/>.
        /// </summary>
        /// <param name="context">The request and response information.</param>
        /// <param name="file">The file to be served.</param>
        public StaticFileResponseContext(HttpContext context, IFileInfo file)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        /// <summary>
        /// The request and response information.
        /// </summary>
        public HttpContext Context { get; }

        /// <summary>
        /// The file to be served.
        /// </summary>
        public IFileInfo File { get; }
    }
}
