// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public abstract class AuthenticationMiddleware<TOptions> where TOptions : AuthenticationOptions
    {
        private readonly RequestDelegate _next;

        protected AuthenticationMiddleware([NotNull] RequestDelegate next, [NotNull] TOptions options)
        {
            Options = options;
            _next = next;
        }

        public TOptions Options { get; set; }

        public async Task Invoke(HttpContext context)
        {
            AuthenticationHandler<TOptions> handler = CreateHandler();
            await handler.Initialize(Options, context);
            if (!await handler.InvokeAsync())
            {
                await _next(context);
            }
            await handler.TeardownAsync();
        }

        protected abstract AuthenticationHandler<TOptions> CreateHandler();
    }
}
