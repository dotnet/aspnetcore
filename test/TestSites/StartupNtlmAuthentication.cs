// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace TestSites
{
    public class StartupNtlmAuthentication
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseIISPlatformHandler();
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
                        return context.Authentication.ChallengeAsync();
                    }
                }

                if (context.Request.Path.Equals("/Forbidden"))
                {
                    return context.Authentication.ForbidAsync(string.Empty);
                }

                if (context.Request.Path.Equals("/AutoForbid"))
                {
                    return context.Authentication.ChallengeAsync();
                }

                if (context.Request.Path.Equals("/RestrictedNegotiate"))
                {
                    if (string.Equals("Negotiate", context.User.Identity.AuthenticationType, System.StringComparison.Ordinal))
                    {
                        return context.Response.WriteAsync("Negotiate");
                    }
                    else
                    {
                        return context.Authentication.ChallengeAsync("Negotiate");
                    }
                }

                if (context.Request.Path.Equals("/RestrictedNTLM"))
                {
                    if (string.Equals("NTLM", context.User.Identity.AuthenticationType, System.StringComparison.Ordinal))
                    {
                        return context.Response.WriteAsync("NTLM");
                    }
                    else
                    {
                        return context.Authentication.ChallengeAsync("NTLM");
                    }
                }

                return context.Response.WriteAsync("Hello World");
            });
        }
    }
}