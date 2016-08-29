// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// The UrlRewrite Context contains the HttpContext of the request, the file provider, and the logger.
    /// There is also a shared string builder across the application of rules.
    /// </summary>
    public class RewriteContext
    {
        public HttpContext HttpContext { get; set; }
        public IFileProvider StaticFileProvider { get; set; }
        public ILogger Logger { get; set; }
        public RuleTermination Result { get; set; }
        // PERF: share the same string builder per request
        internal StringBuilder Builder { get; set; } = new StringBuilder(64);
    }
}
