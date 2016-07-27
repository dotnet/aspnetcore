// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// The UrlRewrite Context contains the HttpContext of the request and the file provider to check conditions.
    /// </summary>
    public class RewriteContext
    {
        public HttpContext HttpContext { get; set; }
        public IFileProvider FileProvider { get; set; }
    }
}
