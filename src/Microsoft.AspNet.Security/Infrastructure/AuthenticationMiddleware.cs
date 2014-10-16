// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Security.Infrastructure
{
    public abstract class AuthenticationMiddleware<TOptions> where TOptions : AuthenticationOptions, new()
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _services;

        protected AuthenticationMiddleware([NotNull] RequestDelegate next, [NotNull] IServiceProvider services, [NotNull] IOptions<TOptions> options, ConfigureOptions<TOptions> configureOptions)
        {
            if (configureOptions != null)
            {
                Options = options.GetNamedOptions(configureOptions.Name);
                configureOptions.Configure(Options, configureOptions.Name);
            }
            else
            {
                Options = options.Options;
            }
            _next = next;
            _services = services;
        }

        public string AuthenticationType { get; set; }

        public TOptions Options { get; set; }

        public async Task Invoke(HttpContext context)
        {
            using (RequestServicesContainer.EnsureRequestServices(context, _services))
            {
                AuthenticationHandler<TOptions> handler = CreateHandler();
                await handler.Initialize(Options, context);
                if (!await handler.InvokeAsync())
                {
                    await _next(context);
                }
                await handler.TeardownAsync();
            }
        }

        protected abstract AuthenticationHandler<TOptions> CreateHandler();
    }
}