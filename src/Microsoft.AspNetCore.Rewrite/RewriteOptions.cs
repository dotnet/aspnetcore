// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Options for the <see cref="RewriteMiddleware"/> 
    /// </summary>
    public class RewriteOptions
    {
        // TODO doc comments
        public IList<Rule> Rules { get; } = new List<Rule>();
        public IFileProvider StaticFileProvider { get; set; }
    }
}
