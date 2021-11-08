// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServerComparison.TestSites;

public class StartupNtlmAuthentication
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        // https://github.com/dotnet/aspnetcore/issues/11462
        // services.AddSingleton<IClaimsTransformation, OneTransformPerRequest>();

        // This will deffer to the server implementations when available.
        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
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

        app.UseAuthentication();
        app.Run((context) =>
        {
            if (context.Request.Path.Equals("/Anonymous"))
            {
                return context.Response.WriteAsync("Anonymous?" + !context.User.Identity.IsAuthenticated);
            }

            if (context.Request.Path.Equals("/Restricted"))
            {
                if (context.User.Identity.IsAuthenticated)
                {
                    return context.Response.WriteAsync("Authenticated");
                }
                else
                {
                    return context.ChallengeAsync();
                }
            }

            if (context.Request.Path.Equals("/Forbidden"))
            {
                return context.ForbidAsync();
            }

            return context.Response.WriteAsync("Hello World");
        });
    }
}
