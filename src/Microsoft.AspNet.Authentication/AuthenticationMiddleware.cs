// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.AspNet.Authentication
{
    public abstract class AuthenticationMiddleware<TOptions> where TOptions : AuthenticationOptions, new()
    {
        private readonly RequestDelegate _next;

        protected AuthenticationMiddleware(
            RequestDelegate next,
            TOptions options,
            ILoggerFactory loggerFactory,
            IUrlEncoder encoder)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            Options = options;
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
            await handler.InitializeAsync(Options, context, Logger, UrlEncoder);
            try
            {
                if (!await handler.HandleRequestAsync())
                {
                    await _next(context);
                }
            }
            catch (Exception)
            {
                try
                {
                    await handler.TeardownAsync();
                }
                catch (Exception)
                {
                    // Don't mask the original exception
                }
                throw;
            }
        }

        protected abstract AuthenticationHandler<TOptions> CreateHandler();
    }
}