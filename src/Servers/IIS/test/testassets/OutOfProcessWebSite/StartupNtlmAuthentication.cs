// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Logging;

namespace TestSites
{
    public class StartupNtlmAuthentication
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            // Simple error page without depending on Diagnostics.
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    if (context.Response.HasStarted)
                    {
                        throw;
                    }

                    context.Response.Clear();
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.ToString());
                }
            });

            app.Use((context, next) =>
            {
                if (context.Request.Path.Equals("/Anonymous"))
                {
                    return context.Response.WriteAsync("Anonymous?" + !context.User.Identity.IsAuthenticated);
                }

                if (context.Request.Path.Equals("/Restricted"))
                {
                    if (context.User.Identity.IsAuthenticated)
                    {
                        return context.Response.WriteAsync(context.User.Identity.AuthenticationType);
                    }
                    else
                    {
                        return context.ChallengeAsync(IISDefaults.AuthenticationScheme);
                    }
                }

                if (context.Request.Path.Equals("/Forbidden"))
                {
                    return context.ForbidAsync(IISDefaults.AuthenticationScheme);
                }

                if (context.Request.Path.Equals("/RestrictedNTLM"))
                {
                    if (string.Equals("NTLM", context.User.Identity.AuthenticationType, StringComparison.Ordinal))
                    {
                        return context.Response.WriteAsync("NTLM");
                    }
                    else
                    {
                        return context.ChallengeAsync(IISDefaults.AuthenticationScheme);
                    }
                }

                return context.Response.WriteAsync("Hello World");
            });
        }
    }
}
