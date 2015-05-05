// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication
{
    public abstract class AuthenticationMiddleware<TOptions> where TOptions : AuthenticationOptions, new()
    {
        private readonly RequestDelegate _next;

        protected AuthenticationMiddleware(
            [NotNull] RequestDelegate next, 
            [NotNull] IOptions<TOptions> options, 
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IUrlEncoder encoder,
            ConfigureOptions<TOptions> configureOptions)
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
            Logger = loggerFactory.CreateLogger(this.GetType().FullName);
            UrlEncoder = encoder;

            if (string.IsNullOrEmpty(Options.ClaimsIssuer))
            {
                // Default to something reasonable
                Options.ClaimsIssuer = Options.AuthenticationScheme;
            }

            _next = next;
        }

        public string AuthenticationScheme { get; set; }

        public TOptions Options { get; set; }

        public ILogger Logger { get; set; }

        public IUrlEncoder UrlEncoder { get; set; }

        public async Task Invoke(HttpContext context)
        {
            var handler = CreateHandler();
            await handler.Initialize(Options, context, Logger, UrlEncoder);
            try
            {
                if (!await handler.InvokeAsync())
                {
                    await _next(context);
                }
            }
            catch (Exception)
            {
                try
                {
                    handler.Faulted = true;
                    await handler.TeardownAsync();
                }
                catch (Exception)
                {
                    // Don't mask the original exception
                }
                throw;
            }
            await handler.TeardownAsync();
        }

        protected abstract AuthenticationHandler<TOptions> CreateHandler();
    }
}