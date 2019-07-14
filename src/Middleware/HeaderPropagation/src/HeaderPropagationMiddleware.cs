// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A Middleware for propagating headers to a <see cref="HttpClient"/>.
    /// </summary>
    public class HeaderPropagationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHeaderPropagationProcessor _processor;

        public HeaderPropagationMiddleware(RequestDelegate next, IHeaderPropagationProcessor processor)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        public Task Invoke(HttpContext context)
        {
            _processor.ProcessRequest(context.Request.Headers);

            return _next.Invoke(context);
        }
    }
}
