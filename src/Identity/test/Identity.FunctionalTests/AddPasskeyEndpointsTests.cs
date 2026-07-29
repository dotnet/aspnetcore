// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public class AddPasskeyEndpointsTests : LoggedTest
{
    private const string PasskeyEndpointsPath = "/.well-known/passkey-endpoints";
    private const string LoggerCategory = "Microsoft.AspNetCore.Identity.PasskeyEndpoints";

    private static Uri BaseAddress { get; } = new Uri("http://example.com");

    [Fact]
    public async Task IsNotServedWhenAddPasskeyEndpointsIsNeverCalled()
    {
        await using var app = await CreateAppAsync(configure: null);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DoesNotShadowAnApplicationsOwnEndpointWhenNotRegistered()
    {
        // Early adopters may already serve a hand-written document. Nothing is registered unless
        // AddPasskeyEndpoints is called, so theirs keeps working.
        await using var app = await CreateAppAsync(
            configure: null,
            configureApp: app => app.MapGet(PasskeyEndpointsPath, () => Results.Text("""{"enroll":"/custom"}""", "application/json")));
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"/custom"}""", body);
    }

    [Fact]
    public async Task ServesTheDocumentWhenConfigured()
    {
        await using var app = await CreateAppAsync(options =>
        {
            options.Enroll = "/Account/Manage/Passkeys";
            options.Manage = "/Account/Manage/Passkeys";
        });
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            """{"enroll":"http://example.com/Account/Manage/Passkeys","manage":"http://example.com/Account/Manage/Passkeys"}""",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ServesTheDocumentGivenOnlyAnEnrollmentPage()
    {
        await using var app = await CreateAppAsync(options => options.Enroll = "/Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", body);
        Assert.DoesNotContain("manage", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ServesTheDocumentGivenOnlyAManagementPage()
    {
        await using var app = await CreateAppAsync(options => options.Manage = "/Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"manage":"http://example.com/Account/Manage/Passkeys"}""", body);
        Assert.DoesNotContain("enroll", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogsWarningAndServesNothingWhenNoEndpointIsConfigured()
    {
        // Advertising an empty document would claim passkey support without giving a credential
        // manager anywhere to send the user.
        await using var app = await CreateAppAsync(options => { });
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);

        var write = Assert.Single(PasskeyEndpointsLogs);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal("NoPasskeyEndpointsConfigured", write.EventId.Name);
    }

    [Fact]
    public async Task TreatsWhitespaceOnlyValuesAsUnset()
    {
        await using var app = await CreateAppAsync(options =>
        {
            options.Enroll = "   ";
            options.Manage = "/Account/Manage/Passkeys";
        });
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"manage":"http://example.com/Account/Manage/Passkeys"}""", body);
        Assert.DoesNotContain("enroll", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogsWarningAndServesNothingWhenEveryValueIsWhitespace()
    {
        // The unconfigured gate and the URL resolution have to agree on what counts as unset, or a
        // whitespace value would suppress the warning and then advertise a URL pointing at nothing.
        await using var app = await CreateAppAsync(options =>
        {
            options.Enroll = " ";
            options.Manage = "\t";
        });
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);

        var write = Assert.Single(PasskeyEndpointsLogs);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal("NoPasskeyEndpointsConfigured", write.EventId.Name);
    }

    [Fact]
    public async Task TrimsSurroundingWhitespaceFromRelativePaths()
    {
        await using var app = await CreateAppAsync(options => options.Enroll = "  /Account/Manage/Passkeys  ");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", body);
    }

    [Fact]
    public async Task TrimsSurroundingWhitespaceFromAbsoluteUrls()
    {
        await using var app = await CreateAppAsync(options => options.Enroll = "  https://accounts.contoso.com/passkeys  ");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"https://accounts.contoso.com/passkeys"}""", body);
    }

    [Fact]
    public async Task ResolvesRelativePathsWithoutLeadingSlash()
    {
        await using var app = await CreateAppAsync(options => options.Enroll = "Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", body);
    }

    [Fact]
    public async Task ResolvesRelativePathsAgainstTheRequestRatherThanTheServerDomain()
    {
        // The relying party identifier can be a registrable suffix of the origin serving the pages,
        // so it must not be used to resolve them.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services => services.Configure<IdentityPasskeyOptions>(options => options.ServerDomain = "example.com"));
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync("http://id.example.com" + PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://id.example.com/Account/Manage/Passkeys"}""", body);
    }

    [Fact]
    public async Task AdvertisesAbsoluteUrlsUnchanged()
    {
        await using var app = await CreateAppAsync(options =>
        {
            options.Enroll = "https://accounts.contoso.com/passkeys/create?source=upgrade";
            options.Manage = "https://accounts.contoso.com/passkeys";
        });
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal(
            """{"enroll":"https://accounts.contoso.com/passkeys/create?source=upgrade","manage":"https://accounts.contoso.com/passkeys"}""",
            body);
    }

    [Fact]
    public async Task ResolvesRelativePathsPerRequestHost()
    {
        await using var app = await CreateAppAsync(options => options.Enroll = "/Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        var first = await client.GetStringAsync("http://first.example.com" + PasskeyEndpointsPath);
        var second = await client.GetStringAsync("http://second.example.com" + PasskeyEndpointsPath);
        var firstAgain = await client.GetStringAsync("http://first.example.com" + PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://first.example.com/Account/Manage/Passkeys"}""", first);
        Assert.Equal("""{"enroll":"http://second.example.com/Account/Manage/Passkeys"}""", second);
        Assert.Equal(first, firstAgain);
    }

    [Fact]
    public async Task ComposesWithOptionsConfiguredElsewhere()
    {
        await using var app = await CreateAppAsync(
            options => options.Manage = "/Account/Manage/Passkeys",
            services => services.Configure<PasskeyEndpointsOptions>(options => options.Enroll = "/Account/Manage/Passkeys"));
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal(
            """{"enroll":"http://example.com/Account/Manage/Passkeys","manage":"http://example.com/Account/Manage/Passkeys"}""",
            body);
    }

    [Fact]
    public async Task AllowsAnonymousRequestsGivenFallbackAuthorizationPolicy()
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services =>
            {
                services.AddAuthentication("Cookies").AddCookie("Cookies");
                services.AddAuthorizationBuilder()
                    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
            },
            configureApp: app => app.MapGet("/protected", () => "protected"));
        using var client = app.GetTestClient();

        // The control endpoint proves the fallback policy is enforced, and shows the redirect that
        // the specification forbids for the passkey endpoints document. WebApplication injects the
        // authentication and authorization middleware itself, so the pipeline below does not add
        // them; a 302 to the cookie login path is only possible if authorization actually ran.
        var protectedResponse = await client.GetAsync("/protected");

        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Contains("/Account/Login", protectedResponse.Headers.Location?.OriginalString, StringComparison.Ordinal);

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task IsServedAtTheOriginRootGivenUsePathBase()
    {
        // The middleware runs before UsePathBase, so the document stays where the specification
        // requires it rather than moving under the prefix.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            usePathBase: "/myapp");
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/myapp" + PasskeyEndpointsPath)).StatusCode);
    }

    [Fact]
    public async Task RelativePathsIgnoreAPathBaseAddedByThePipeline()
    {
        // A documented limitation: UsePathBase runs after this middleware, so applications that set
        // a path base in the pipeline have to advertise absolute URLs.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            usePathBase: "/myapp");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", body);
    }

    [Fact]
    public async Task RelativePathsIncludeAPathBaseSetByTheServer()
    {
        // A server-hosted virtual directory populates the path base before the pipeline runs, so it
        // is visible here and belongs in the advertised URL.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            serverPathBase: "/virtualdir");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync("/virtualdir" + PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://example.com/virtualdir/Account/Manage/Passkeys"}""", body);
    }

    [Fact]
    public async Task AbsoluteUrlsAreUnaffectedByAPathBaseSetByTheServer()
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "https://accounts.contoso.com/passkeys",
            serverPathBase: "/virtualdir");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync("/virtualdir" + PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"https://accounts.contoso.com/passkeys"}""", body);
    }

    [Fact]
    public async Task DoesNotHandleOtherMethodsAtTheSamePath()
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            configureApp: app => app.MapPost(PasskeyEndpointsPath, () => "posted"));
        using var client = app.GetTestClient();

        var response = await client.PostAsync(PasskeyEndpointsPath, content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("posted", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LogsWarningOncePerAppGivenServerDomainMismatch()
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services => services.Configure<IdentityPasskeyOptions>(options => options.ServerDomain = "contoso.com"));
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);

        var write = Assert.Single(PasskeyEndpointsLogs);
        Assert.Equal(LogLevel.Warning, write.LogLevel);
        Assert.Equal("PasskeyEndpointsServerDomainMismatch", write.EventId.Name);
        Assert.Contains("contoso.com", write.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DoesNotLogGivenMatchingServerDomain()
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services => services.Configure<IdentityPasskeyOptions>(options => options.ServerDomain = "example.com"));
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);

        Assert.Empty(PasskeyEndpointsLogs);
    }

    private IEnumerable<WriteContext> PasskeyEndpointsLogs
        => TestSink.Writes.Where(w => w.LoggerName == LoggerCategory);

    private async Task<WebApplication> CreateAppAsync(
        Action<PasskeyEndpointsOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null,
        Action<WebApplication>? configureApp = null,
        string? usePathBase = null,
        string? serverPathBase = null)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer(options =>
        {
            options.BaseAddress = BaseAddress;
        });
        builder.Services.AddSingleton(LoggerFactory);
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        if (serverPathBase is not null)
        {
            // Startup filters run in registration order, so registering this one first makes the
            // path base visible to the passkey endpoints middleware, as a server would.
            builder.Services.AddSingleton<IStartupFilter>(new ServerPathBaseStartupFilter(serverPathBase));
        }

        configureServices?.Invoke(builder.Services);

        if (configure is not null)
        {
            builder.Services.AddPasskeyEndpoints(configure);
        }

        var app = builder.Build();

        if (usePathBase is not null)
        {
            app.UsePathBase(usePathBase);

            // Routing has to be added explicitly here so that it runs after UsePathBase.
            app.UseRouting();
        }

        // UseAuthentication and UseAuthorization are deliberately not called. WebApplication injects
        // both when the matching services are registered, and defers them until after routing when
        // routing is added explicitly, so they observe the matched endpoint. Calling them here would
        // set the flags that suppress that injection, moving responsibility for their placement into
        // this helper for no benefit.
        configureApp?.Invoke(app);

        await app.StartAsync();

        return app;
    }

    private sealed class ServerPathBaseStartupFilter(string pathBase) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => builder =>
            {
                builder.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Path.StartsWithSegments(pathBase, out var remaining))
                    {
                        context.Request.PathBase = pathBase;
                        context.Request.Path = remaining;
                    }

                    await nextMiddleware(context);
                });

                next(builder);
            };
    }
}
