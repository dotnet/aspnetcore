// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromSeconds(20);
        options.Events = new CookieAuthenticationEvents()
        {
            OnCheckSlidingExpiration = context =>
            {
                // If 25% expired instead of the default 50%.
                context.ShouldRenew = context.ElapsedTime > (context.Options.ExpireTimeSpan / 4);

                // Don't renew on API endpoints that use JWT.
                var authData = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAuthorizeData>();
                if (authData != null && string.Equals(authData.AuthenticationSchemes, "Bearer", StringComparison.Ordinal))
                {
                    context.ShouldRenew = false;
                }

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

app.UseAuthentication();

app.MapGet("/", async context =>
{
    if (!context.User.Identities.Any(identity => identity.IsAuthenticated))
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "bob") }, CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user);

        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Hello First timer");
        return;
    }

    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("Hello old timer");
});

app.MapGet("/ticket", async context =>
{
    var ticket = await context.AuthenticateAsync();
    if (!ticket.Succeeded)
    {
        await context.Response.WriteAsync($"Signed Out");
        return;
    }

    foreach (var (key, value) in ticket.Properties.Items)
    {
        await context.Response.WriteAsync($"{key}: {value}\r\n");
    }
});

app.Run();
