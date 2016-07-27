// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Internal;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Options for the <see cref="RewriteMiddleware"/> 
    /// </summary>
    public class RewriteOptions
    {
        /// <summary>
        /// The ordered list of rules to apply to the context.
        /// </summary>
        public List<Rule> Rules { get; set; } = new List<Rule>();
        public IFileProvider FileProvider { get; set; }
    }
}
