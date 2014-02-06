// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.Owin.FileSystems;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.StaticFiles.Infrastructure
{
    /// <summary>
    /// Options common to several middleware components
    /// </summary>
    public class SharedOptions
    {
        private PathString _requestPath;

        /// <summary>
        /// Defaults to all request paths and the current physical directory.
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
        public IFileSystem FileSystem { get; set; }
    }
}
