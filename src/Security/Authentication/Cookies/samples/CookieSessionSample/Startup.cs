// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace CookieSessionSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // This can be removed after https://github.com/aspnet/IISIntegration/issues/371
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie();

        services.AddSingleton<ITicketStore, MemoryCacheTicketStore>();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((o, ticketStore) => o.SessionStore = ticketStore);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseAuthentication();

        app.Run(async context =>
        {
            if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
            {
                // Make a large identity
                var claims = new List<Claim>(1001);
                claims.Add(new Claim(ClaimTypes.Name, "bob"));
                for (int i = 0; i < 1000; i++)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "SomeRandomGroup" + i, ClaimValueTypes.String, "IssuedByBob", "OriginalIssuerJoe"));
                }

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello First timer");
                return;
            }

            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Hello old timer");
        });
    }
}
