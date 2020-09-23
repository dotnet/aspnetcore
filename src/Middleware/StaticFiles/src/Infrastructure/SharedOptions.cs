// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles.Infrastructure
{
    /// <summary>
    /// Options common to several middleware components
    /// </summary>
    public class SharedOptions
    {
        private PathString _requestPath;

        /// <summary>
        /// Defaults to all request paths.
        /// </summary>
        public SharedOptions()
        {
            RequestPath = PathString.Empty;
        }

        /// <summary>
        /// The request path that maps to static resources
        /// </summary>
        public PathString RequestPath
        {
            get { return _requestPath; }
            set
            {
                if (value.HasValue && value.Value.EndsWith("/", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Request path must not end in a slash");
                }
                _requestPath = value;
            }
        }

        /// <summary>
        /// The file system used to locate resources
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <summary>
        /// Indicates whether to redirect to add a trailing slash at the end of path. Relative resource links may require this.
        /// </summary>
        public bool RedirectToAppendTrailingSlash { get; set; } = true;
    }
}
