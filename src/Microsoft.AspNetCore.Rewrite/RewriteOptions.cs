// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Options for the <see cref="RewriteMiddleware"/> 
    /// </summary>
    public class RewriteOptions
    {
        /// <summary>
        /// A list of <see cref="Rule"/> that will be applied in order upon a request.
        /// </summary>
        public IList<Rule> Rules { get; } = new List<Rule>();

        /// <summary>
        /// Gets and sets the File Provider for file and directory checks. Defaults to <see cref="IHostingEnvironment.WebRootFileProvider"/>
        /// </summary>
        public IFileProvider StaticFileProvider { get; set; }
    }
}
