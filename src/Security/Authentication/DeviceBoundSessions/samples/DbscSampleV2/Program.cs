// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.DeviceBoundSessions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly DbscDebugState _debug = new();

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_debug);

        services.AddAuthentication()
            // The source cookie scheme (long-lived sign-in cookie)
            .AddCookie(DbscNames.Source, o =>
            {
                o.Cookie.Name = DbscNames.SourceCookie;
                o.LoginPath = "/login";
                o.ExpireTimeSpan = TimeSpan.FromDays(7);
            })
            // The DBSC handler + refresh/session cookie schemes + policy scheme.
            // The session TTL is read live from the debug state so it can be changed at runtime.
            .AddDeviceBoundSession(DbscNames.Source, o =>
            {
                o.ShortLivedCookieExpiration = _debug.SessionTtl;

                // Exclude the debug dashboard's own endpoints from the DBSC session scope so that
                // dashboard polling (/debug/state, /debug/log) is NOT treated as in-scope traffic.
                // This keeps the browser from enforcing the bound cookie or triggering refreshes for
                // the inspector itself, cleanly separating debugging requests from real test requests.
                o.ScopeSpecifications.Add(new DeviceBoundSessionScopeRule
                {
                    Type = "exclude",
                    Domain = "*",
                    Path = "/debug",
                });
            });

        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        // Write all HTTP traffic to a HAR file (raw, for external tooling)
        var harPath = Path.Combine(AppContext.BaseDirectory, "dbsc-v2-traffic.har");
        Console.WriteLine($"[HAR] Writing traffic to: {harPath}");
        app.UseMiddleware<HarLoggingMiddleware>(harPath);

        // Capture decoded exchanges for the live dashboard
        app.UseMiddleware<DebugCaptureMiddleware>();

        app.UseRouting();
        app.UseAuthentication();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(Dashboard.Html);
            });

            endpoints.MapGet("/login", context =>
            {
                context.Response.Redirect("/");
                return Task.CompletedTask;
            });

            endpoints.MapPost("/login", async context =>
            {
                var form = await context.Request.ReadFormAsync();
                var username = form["username"].ToString();
                if (string.IsNullOrEmpty(username))
                {
                    context.Response.Redirect("/");
                    return;
                }

                // Authorization gate (demo): only "alice" is allowed. Anyone else (e.g. "bob")
                // fails login: we do NOT sign in (so no source cookie and no DBSC registration),
                // and we redirect back to "/" so the browser stays on the dashboard, logged out.
                if (!string.Equals(username, "alice", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect($"/?loginError={Uri.EscapeDataString(username)}");
                    return;
                }

                // Apply a runtime-configurable session TTL before sign-in triggers registration.
                if (int.TryParse(form["ttl"], out var ttlSeconds) && ttlSeconds is > 0 and <= 86400)
                {
                    _debug.SessionTtl = TimeSpan.FromSeconds(ttlSeconds);
                    // Force the DBSC options to be rebuilt so the new TTL is picked up.
                    context.RequestServices
                        .GetRequiredService<IOptionsMonitorCache<DeviceBoundSessionOptions>>()
                        .Clear();
                }

                var identity = new ClaimsIdentity(DbscNames.Source);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));
                identity.AddClaim(new Claim(ClaimTypes.Name, username));

                await context.SignInAsync(
                    DbscNames.Source,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true });

                context.Response.Redirect("/");
            });

            endpoints.MapGet("/api/time", async context =>
            {
                if (context.User.Identity?.IsAuthenticated != true)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }
                await context.Response.WriteAsync($"Server time {DateTime.UtcNow:HH:mm:ss.fff} | user {context.User.Identity!.Name}");
            });

            endpoints.MapGet("/signout", async context =>
            {
                await SignOutAllAsync(context);
                context.Response.Redirect("/");
            });

            // Full reset: delete every cookie AND clear the captured log to observe a fresh registration.
            endpoints.MapGet("/clear", async context =>
            {
                await SignOutAllAsync(context);
                _debug.ClearLog();
                context.Response.Redirect("/");
            });

            endpoints.MapPost("/debug/clearlog", context =>
            {
                _debug.ClearLog();
                context.Response.StatusCode = 204;
                return Task.CompletedTask;
            });

            endpoints.MapGet("/debug/state", async context =>
            {
                var cookies = DbscDecoder.DecodeRequestCookies(context);
                await context.Response.WriteAsJsonAsync(new
                {
                    authenticated = context.User.Identity?.IsAuthenticated == true,
                    user = context.User.Identity?.Name,
                    ttlSeconds = _debug.SessionTtl.TotalSeconds,
                    cookies,
                });
            });

            endpoints.MapGet("/debug/log", async context =>
            {
                long since = 0;
                if (long.TryParse(context.Request.Query["since"], out var s))
                {
                    since = s;
                }
                // Long poll: hold the request open until the log changes or ~60s elapses.
                var (lastId, entries) = await _debug.WaitForChangesAsync(
                    since, TimeSpan.FromSeconds(60), context.RequestAborted);
                await context.Response.WriteAsJsonAsync(new { lastId, entries });
            });
        });
    }

    private static async Task SignOutAllAsync(HttpContext context)
    {
        await context.SignOutAsync(DbscNames.Source);
        await context.SignOutAsync(DbscNames.Refresh);
        await context.SignOutAsync(DbscNames.Session);
    }
}
