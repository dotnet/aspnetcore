// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.DeviceBoundSessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DbscSampleV2;

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
                        options.ListenLocalhost(7299, listenOptions =>
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
                factory.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
            })
            .Build();

        return host.RunAsync();
    }
}

public class Startup
{
    private const string SourceScheme = "Application";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication()
            // The source cookie scheme (long-lived sign-in cookie)
            .AddCookie(SourceScheme, o =>
            {
                o.Cookie.Name = ".AspNetCore.Application";
                o.LoginPath = "/login";
                o.ExpireTimeSpan = TimeSpan.FromDays(7);
            })
            // The DBSC handler + refresh/session cookie schemes + policy scheme
            .AddDeviceBoundSession(SourceScheme, o =>
            {
                o.ShortLivedCookieExpiration = TimeSpan.FromSeconds(30);
            });

        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Write all HTTP traffic to a HAR file
        var harPath = Path.Combine(AppContext.BaseDirectory, "dbsc-v2-traffic.har");
        Console.WriteLine($"[HAR] Writing traffic to: {harPath}");
        app.UseMiddleware<HarLoggingMiddleware>(harPath);

        app.UseRouting();
        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/login", async context =>
            {
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("""
                    <!DOCTYPE html>
                    <html>
                    <head><title>DBSC v2 Sample - Login</title></head>
                    <body>
                        <h1>Device Bound Session Credentials v2 - Test App</h1>
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

                var identity = new ClaimsIdentity(SourceScheme);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));
                identity.AddClaim(new Claim(ClaimTypes.Name, username));

                // Sign in to the source scheme.
                await context.SignInAsync(
                    SourceScheme,
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
                    "<!DOCTYPE html><html><head><title>DBSC v2</title></head><body>" +
                    $"<h1>Welcome, {userName}!</h1>" +
                    "<p>Authenticated with Device Bound Session Credentials (v2 architecture).</p>" +
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
                // Sign out all schemes
                await context.SignOutAsync(SourceScheme);
                context.Response.Redirect("/login");
            });
        });
    }
}
