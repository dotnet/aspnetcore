// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics
{
    public class StatusCodeContext
    {
        public StatusCodeContext(HttpContext context, StatusCodePagesOptions options, RequestDelegate next)
        {
            HttpContext = context;
            Options = options;
            Next = next;
        }

        public HttpContext HttpContext { get; }

        public StatusCodePagesOptions Options { get; }

        public RequestDelegate Next { get; }
    }
}
