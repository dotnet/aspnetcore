// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.Owin.FileSystems;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Contains information about the request and the file that will be served in response.
    /// </summary>
    public class StaticFileResponseContext
    {
        /// <summary>
        /// The request and response information.
        /// </summary>
        public HttpContext Context { get; internal set; }

        /// <summary>
        /// The file to be served.
        /// </summary>
        public IFileInfo File { get; internal set; }
    }
}
