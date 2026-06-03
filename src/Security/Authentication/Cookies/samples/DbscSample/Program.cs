// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DbscSample;

public static class Program
{
    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(options =>
                    {
                        options.ListenLocalhost(7298, listenOptions =>
                        {
                            listenOptions.UseHttps();
                        });
                    })
                    .UseStartup<Startup>();
            })
            .ConfigureLogging(factory =>
            {
                factory.AddConsole();
                factory.AddFilter("Default", LogLevel.Information);
                factory.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);
                factory.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            })
            .Build();

        return host.RunAsync();
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.DeviceBoundSession = new DeviceBoundSessionOptions
                {
                    Enabled = true,
                    // Short expiration for testing (refreshes happen every 30s)
                    ShortLivedCookieExpiration = TimeSpan.FromSeconds(30),
                };
            });

        services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
            logging.RequestHeaders.Add("Sec-Session-Id");
            logging.RequestHeaders.Add("Sec-Secure-Session-Id");
            logging.RequestHeaders.Add("Secure-Session-Response");
            logging.ResponseHeaders.Add("Secure-Session-Registration");
            logging.ResponseHeaders.Add("Secure-Session-Challenge");
            logging.ResponseHeaders.Add("Set-Cookie");
            logging.RequestBodyLogLimit = 4096;
            logging.ResponseBodyLogLimit = 4096;
            logging.CombineLogs = true;
        });

        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseHttpLogging();
        app.UseRouting();
        app.UseAuthentication();
        app.UseDeviceBoundSessions();

        // DEBUG: dump authenticated ticket properties into response headers
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    context.Response.Headers["X-Debug-Principal"] = context.User.Identity.Name ?? "(no name)";

                    // Get the auth ticket from the feature
                    var authFeature = context.Features.Get<Microsoft.AspNetCore.Authentication.IAuthenticateResultFeature>();
                    var ticket = authFeature?.AuthenticateResult?.Ticket;
                    if (ticket is not null)
                    {
                        context.Response.Headers["X-Debug-Scheme"] = ticket.AuthenticationScheme;
                        foreach (var kvp in ticket.Properties.Items)
                        {
                            var headerKey = kvp.Key.Replace(".", "-").Replace("/", "_");
                            context.Response.Headers[$"X-Debug-Prop-{headerKey}"] = kvp.Value ?? "(null)";
                        }
                    }
                    else
                    {
                        context.Response.Headers["X-Debug-Ticket"] = "authenticated-but-no-ticket-feature";
                    }
                }
                else
                {
                    context.Response.Headers["X-Debug-Auth"] = "not-authenticated";
                }
                return Task.CompletedTask;
            });

            await next();
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/login", async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("""
                    <!DOCTYPE html>
                    <html>
                    <head><title>DBSC Sample - Login</title></head>
                    <body>
                        <h1>Device Bound Session Credentials - Test App</h1>
                        <form method="post" action="/login">
                            <label>Username: <input name="username" value="alice" /></label>
                            <button type="submit">Sign In</button>
                        </form>
                    </body>
                    </html>
                    """);
            });

            endpoints.MapPost("/login", async context =>
            {
                var form = await context.Request.ReadFormAsync();
                var username = form["username"].ToString();

                if (string.IsNullOrEmpty(username))
                {
                    context.Response.Redirect("/login");
                    return;
                }

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.Name, username));
                identity.AddClaim(new Claim(ClaimTypes.Email, $"{username}@example.com"));

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true });

                context.Response.Redirect("/");
            });

            endpoints.MapGet("/", async context =>
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    context.Response.Redirect("/login");
                    return;
                }

                var userName = context.User.Identity!.Name;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(
                    "<!DOCTYPE html><html><head><title>DBSC Sample</title></head><body>" +
                    $"<h1>Welcome, {userName}!</h1>" +
                    "<p>Authenticated with Device Bound Session Credentials.</p>" +
                    "<h2>How to test:</h2><ol>" +
                    "<li>Open Chrome DevTools (F12) &rarr; Network tab</li>" +
                    "<li>Check the sign-in response for <code>Secure-Session-Registration</code> header</li>" +
                    "<li>Watch for POST to <code>/.well-known/dbsc/registration</code></li>" +
                    "<li>Wait 30s, then make a request to observe refresh at <code>/.well-known/dbsc/refresh</code></li>" +
                    "</ol>" +
                    "<p><a href='/api/time'>API endpoint</a> | <a href='/signout'>Sign Out</a></p>" +
                    "<h2>Auto-refresh (every 10s):</h2><pre id='log'></pre>" +
                    "<script>" +
                    "const log=document.getElementById('log');" +
                    "async function f(){try{const r=await fetch('/api/time');const t=await r.text();" +
                    "log.textContent+='['+new Date().toLocaleTimeString()+'] '+r.status+' - '+t+'\\n';" +
                    "}catch(e){log.textContent+='Error: '+e.message+'\\n';}}" +
                    "setInterval(f,10000);f();</script></body></html>");
            });

            endpoints.MapGet("/api/time", async context =>
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
                await context.Response.WriteAsync($"Server time: {DateTime.UtcNow:O} | User: {context.User.Identity!.Name}");
            });

            endpoints.MapGet("/signout", async context =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                context.Response.Redirect("/login");
            });
        });
    }
}
