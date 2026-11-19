// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
using Microsoft.Extensions.DependencyInjection;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
#endif

// Mirrors the documented policy-scheme path-based forwarding pattern from https://learn.microsoft.com/aspnet/core/security/authentication/policyschemes
public class BackslashPathSeparatorAuthorizationTests : LoggedTest
{
    [Theory]
    [InlineData("/admin/secret", "admin:Strong")]
    [InlineData("/admin%5Csecret", "admin:Strong")]
    [InlineData("/admin\\secret", "admin:Strong")]
    [InlineData("/public", "public:Weak")]
    public async Task SelectorAndEndpointRoutingAgreeOnBackslashSmuggledAdminPaths(string requestTarget, string expectedBody)
    {
        var services = new ServiceCollection();
        services.AddRouting();
        services.AddLogging();
        services.AddSingleton(new DiagnosticListener("BackslashPathSeparatorAuthorizationTests"));

        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        appBuilder.UseRouting();

        // Mirrors PolicySchemeOptions.ForwardDefaultSelector from the documented pattern: a
        // decision made purely from HttpRequest.Path, independent of whatever endpoint routing
        // later matches.
        appBuilder.Use(async (context, next) =>
        {
            var isAdmin = context.Request.Path.StartsWithSegments("/admin");
            context.Items["SelectedScheme"] = isAdmin ? "Strong" : "Weak";
            await next(context);
        });

        appBuilder.UseEndpoints(endpoints =>
        {
            // The protected endpoint: only reachable for paths the real DFA route matcher (not
            // the selector above) recognizes as under /admin.
            endpoints.MapGet("/admin/{**catchall}", async context =>
            {
                await context.Response.WriteAsync($"admin:{context.Items["SelectedScheme"]}");
            });

            // Unprotected fallback, matching everything else.
            endpoints.MapGet("/{**catchall}", async context =>
            {
                await context.Response.WriteAsync($"public:{context.Items["SelectedScheme"]}");
            });
        });

        var serviceContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(appBuilder.Build(), serviceContext))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    $"GET {requestTarget} HTTP/1.1",
                    "Host:",
                    "Connection: close",
                    "",
                    "");

                var response = await connection.Reader.ReadToEndAsync();
                Assert.Contains(expectedBody, response);
            }
        }
    }
}
