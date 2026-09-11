// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0039 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    public async Task IsNotServedWhenMapWellKnownPasskeyEndpointsIsNeverCalled()
    {
        await using var app = await CreateAppAsync(configure: null, map: false);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DoesNotShadowAnApplicationsOwnEndpointWhenNotMapped()
    {
        // Early adopters may already serve a hand-written document. Nothing is served unless
        // MapWellKnownPasskeyEndpoints is called, so theirs keeps working.
        await using var app = await CreateAppAsync(
            configure: null,
            map: false,
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
    public async Task AdvertisesAPrfUsageDetailsPage()
    {
        await using var app = await CreateAppAsync(options =>
        {
            options.Manage = "/Account/Manage/Passkeys";
            options.PrfUsageDetails = "/Help/Passkeys";
        });
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal(
            """{"manage":"http://example.com/Account/Manage/Passkeys","prfUsageDetails":"http://example.com/Help/Passkeys"}""",
            body);
    }

    [Fact]
    public async Task ServesAnEmptyDocumentWhenNoEndpointIsConfigured()
    {
        // Calling the map method is itself the statement that the application supports passkeys, and
        // the specification defines an empty document as signalling exactly that.
        await using var app = await CreateAppAsync(options => { });
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{}", await response.Content.ReadAsStringAsync());
        Assert.Empty(PasskeyEndpointsLogs);
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
    public async Task ServesAnEmptyDocumentWhenEveryValueIsWhitespace()
    {
        await using var app = await CreateAppAsync(options =>
        {
            options.Enroll = " ";
            options.Manage = "\t";
            options.PrfUsageDetails = "  ";
        });
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal("{}", body);
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
    public async Task AdvertisesAbsoluteUrls()
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

    [Theory]
    // Uri accepts these even though they are not well formed, so advertising the configured value
    // verbatim would put a raw space into the document.
    [InlineData("https://accounts.contoso.com/pass keys", "https://accounts.contoso.com/pass%20keys")]
    [InlineData("HTTPS://Accounts.Contoso.COM/passkeys", "https://accounts.contoso.com/passkeys")]
    // A default port and a dot segment are removed, and an authority on its own gains a path, so an
    // absolute value is advertised as it stands only up to normalization.
    [InlineData("https://accounts.contoso.com:443/passkeys", "https://accounts.contoso.com/passkeys")]
    [InlineData("https://accounts.contoso.com", "https://accounts.contoso.com/")]
    [InlineData("https://accounts.contoso.com/passkeys/../passkeys", "https://accounts.contoso.com/passkeys")]
    public async Task NormalizesAbsoluteUrls(string configured, string expected)
    {
        await using var app = await CreateAppAsync(options => options.Enroll = configured);
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal($$"""{"enroll":"{{expected}}"}""", body);
    }

    [Theory]
    // A path is escaped rather than concatenated, because an unescaped '#' would truncate the URL at
    // the fragment and send the user to the wrong page.
    [InlineData("/Account/Pass keys", "http://example.com/Account/Pass%20keys")]
    [InlineData("/passkeys/cr\u00e9er", "http://example.com/passkeys/cr%C3%A9er")]
    // A query and a fragment are preserved rather than escaped into the path, and are escaped in
    // turn, because unlike PathString they are taken to be escaped already.
    [InlineData("/Account/Passkeys?ref=cm", "http://example.com/Account/Passkeys?ref=cm")]
    [InlineData("/Account/Passkeys#create", "http://example.com/Account/Passkeys#create")]
    [InlineData("/Account/Passkeys?ref=cm#create", "http://example.com/Account/Passkeys?ref=cm#create")]
    [InlineData("/Account/Passkeys?return=/my page", "http://example.com/Account/Passkeys?return=/my%20page")]
    [InlineData("/Account/Passkeys#cr\u00e9er", "http://example.com/Account/Passkeys#cr%C3%A9er")]
    [InlineData("/Account/Passkeys?a=b c#fr ag", "http://example.com/Account/Passkeys?a=b%20c#fr%20ag")]
    // Escaping is not applied twice, so a value that is already escaped survives unchanged.
    [InlineData("/Account/Pass%20keys", "http://example.com/Account/Pass%20keys")]
    public async Task EscapesRelativeValues(string configured, string expected)
    {
        await using var app = await CreateAppAsync(options => options.Enroll = configured);
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal($$"""{"enroll":"{{expected}}"}""", body);
    }

    [Theory]
    // Each of these parses as an absolute Uri on every platform, so restricting the absolute branch
    // to http and https is what keeps them out of it. A rooted path like "/Account/Manage/Passkeys"
    // parses the same way on Unix but not on Windows, so these stand in for a case a Windows-only
    // test run cannot reach.
    [InlineData("javascript:alert(1)", "http://example.com/javascript:alert(1)")]
    [InlineData("file:///Account/Manage/Passkeys", "http://example.com/file:///Account/Manage/Passkeys")]
    [InlineData("ftp://accounts.contoso.com/passkeys", "http://example.com/ftp://accounts.contoso.com/passkeys")]
    public async Task DoesNotAdvertiseNonHttpSchemesUnchanged(string configured, string expected)
    {
        await using var app = await CreateAppAsync(options => options.Enroll = configured);
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync(PasskeyEndpointsPath);

        Assert.Equal($$"""{"enroll":"{{expected}}"}""", body);
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
    public async Task ResolvesRelativePathsAgainstForwardedHeaders()
    {
        // The endpoint runs where the application places it, so middleware that corrects the scheme
        // and host of a proxied request is observed. Advertising the internal origin instead would
        // send the user somewhere unreachable.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services => services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
            }),
            configureApp: app => app.UseForwardedHeaders());
        using var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, PasskeyEndpointsPath);
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-Host", "contoso.com");

        using var response = await client.SendAsync(request);

        Assert.Equal(
            """{"enroll":"https://contoso.com/Account/Manage/Passkeys"}""",
            await response.Content.ReadAsStringAsync());
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
    public async Task ServesAnEmptyDocumentWhenAddPasskeyEndpointsIsNeverCalled()
    {
        // Configuring the advertised locations is optional, so mapping the endpoint on its own has
        // to work rather than fail on a missing options registration.
        await using var app = await CreateAppAsync(configure: null);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ServesTheDocumentAsUnparameterizedJsonThatIsNotStored()
    {
        // The specification asks for application/json, and the body is built from the scheme and
        // host of the request, so a shared cache must not reuse it for another origin.
        await using var app = await CreateAppAsync(options => options.Enroll = "/Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData("/.WELL-KNOWN/PASSKEY-ENDPOINTS")]
    [InlineData("/.well-known/passkey-endpoints/")]
    public async Task ServesTheDocumentForPathsRoutingTreatsAsEquivalent(string path)
    {
        // Routing matches paths case-insensitively and ignores a trailing slash, so the document is
        // reachable at more paths than the specification names. Credential managers only request the
        // exact path, so this is pinned rather than corrected.
        await using var app = await CreateAppAsync(options => options.Enroll = "/Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MappingTheEndpointTwiceMakesTheRequestAmbiguous()
    {
        // Mapping the same route twice is ambiguous wherever it happens in routing, and the error
        // names the duplicated route, so no bespoke guard is added here. AmbiguousMatchException is
        // internal to routing, hence the assertion on the name.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            configureApp: app => app.MapWellKnownPasskeyEndpoints());
        using var client = app.GetTestClient();

        var exception = await Record.ExceptionAsync(() => client.GetAsync(PasskeyEndpointsPath));

        Assert.NotNull(exception);
        Assert.Equal("AmbiguousMatchException", exception.GetType().Name);
        Assert.Contains(PasskeyEndpointsPath, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogsErrorWhenMappedIntoARouteGroupWithAPrefix()
    {
        // A prefix moves the document off the root of the origin, which is the only place a
        // credential manager looks for it. The diagnostic cannot be an exception: conventions run
        // while the endpoint data source is enumerated, so throwing there would take down every
        // endpoint in the application rather than just this one.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            map: false,
            configureApp: app =>
            {
                app.MapGet("/unrelated", () => "unrelated");
                app.MapGroup("/api").MapWellKnownPasskeyEndpoints();
            });
        using var client = app.GetTestClient();

        Assert.Equal("unrelated", await client.GetStringAsync("/unrelated"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);

        var log = Assert.Single(PasskeyEndpointsLogs);

        Assert.Equal(LogLevel.Error, log.LogLevel);
        Assert.Contains("/api/.well-known/passkey-endpoints", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogsErrorWhenMappedIntoARouteGroupWithoutAuthorizationServices()
    {
        // Authorization is what resolves the endpoint data source while the pipeline is built, so
        // an application without it only builds endpoints once the first request arrives. That is
        // the case where a throwing convention would turn a misplaced document into an application
        // that answers every request with a 500.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            map: false,
            addAuthorizationServices: false,
            configureApp: app =>
            {
                app.MapGet("/unrelated", () => "unrelated");
                app.MapGroup("/api").MapWellKnownPasskeyEndpoints();
            });
        using var client = app.GetTestClient();

        Assert.Equal("unrelated", await client.GetStringAsync("/unrelated"));

        var log = Assert.Single(PasskeyEndpointsLogs);

        Assert.Equal(LogLevel.Error, log.LogLevel);
        Assert.Contains("/api/.well-known/passkey-endpoints", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogsErrorWhenMappedIntoNestedRouteGroups()
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            map: false,
            configureApp: app => app.MapGroup("/a").MapGroup("/b").MapWellKnownPasskeyEndpoints());
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(PasskeyEndpointsPath)).StatusCode);

        var log = Assert.Single(PasskeyEndpointsLogs);

        Assert.Equal(LogLevel.Error, log.LogLevel);
        Assert.Contains("/a/b/.well-known/passkey-endpoints", log.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IsServedWhenMappedIntoARouteGroupWithoutAPrefix()
    {
        // MapIdentityApi groups its endpoints this way, so an empty prefix has to stay allowed. The
        // guard only logs now, so the absence of a diagnostic is what proves the prefix collapsed.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            map: false,
            configureApp: app => app.MapGroup("").MapWellKnownPasskeyEndpoints());
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", await response.Content.ReadAsStringAsync());
        Assert.Empty(PasskeyEndpointsLogs);
    }

    [Fact]
    public async Task IsServedAtTheOriginRootGivenUsePathBase()
    {
        // Credential managers request the document at the root of the origin, so it has to be served
        // there whether or not the application also answers under its path base.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            usePathBase: "/myapp");
        using var client = app.GetTestClient();

        var response = await client.GetAsync(PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RelativePathsIncludeAPathBaseAddedByThePipeline()
    {
        // The endpoint is matched after UsePathBase, so a path base set there is visible and belongs
        // in the advertised URL.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            usePathBase: "/myapp");
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync("/myapp" + PasskeyEndpointsPath);

        Assert.Equal("""{"enroll":"http://example.com/myapp/Account/Manage/Passkeys"}""", body);
    }

    [Fact]
    public async Task IsServedUnderAPathBaseWithoutAnExplicitCallToUseRouting()
    {
        // WebApplication places its own UseRouting ahead of the pipeline, so UsePathBase would be
        // too late to be seen were it not for UsePathBase re-running routing itself. Both the root
        // of the origin and the path base therefore work in a default pipeline.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            usePathBase: "/myapp",
            useExplicitRouting: false);
        using var client = app.GetTestClient();

        var atRoot = await client.GetAsync(PasskeyEndpointsPath);
        var underPathBase = await client.GetAsync("/myapp" + PasskeyEndpointsPath);

        Assert.Equal(HttpStatusCode.OK, atRoot.StatusCode);
        Assert.Equal("""{"enroll":"http://example.com/Account/Manage/Passkeys"}""", await atRoot.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, underPathBase.StatusCode);
        Assert.Equal(
            """{"enroll":"http://example.com/myapp/Account/Manage/Passkeys"}""",
            await underPathBase.Content.ReadAsStringAsync());
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
    public async Task AnswersHeadRequests()
    {
        await using var app = await CreateAppAsync(options => options.Enroll = "/Account/Manage/Passkeys");
        using var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Head, PasskeyEndpointsPath);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

    [Theory]
    // The relying party identifier may be a registrable suffix of the origin serving the document,
    // so a subdomain of it is a correct configuration rather than a mistake to warn about.
    [InlineData("http://id.example.com")]
    [InlineData("http://accounts.id.example.com")]
    public async Task DoesNotLogGivenAHostBelowTheServerDomain(string requestOrigin)
    {
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services => services.Configure<IdentityPasskeyOptions>(options => options.ServerDomain = "example.com"));
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(requestOrigin + PasskeyEndpointsPath)).StatusCode);

        Assert.Empty(PasskeyEndpointsLogs);
    }

    [Fact]
    public async Task LogsWarningGivenAHostThatMerelyEndsWithTheServerDomain()
    {
        // "notexample.com" ends with "example.com" without being below it, so the suffix check has
        // to require a label boundary.
        await using var app = await CreateAppAsync(
            options => options.Enroll = "/Account/Manage/Passkeys",
            services => services.Configure<IdentityPasskeyOptions>(options => options.ServerDomain = "example.com"));
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("http://notexample.com" + PasskeyEndpointsPath)).StatusCode);

        Assert.Single(PasskeyEndpointsLogs);
    }

    private IEnumerable<WriteContext> PasskeyEndpointsLogs
        => TestSink.Writes.Where(w => w.LoggerName == LoggerCategory);

    private async Task<WebApplication> CreateAppAsync(
        Action<PasskeyEndpointsOptions>? configure = null,
        Action<IServiceCollection>? configureServices = null,
        Action<WebApplication>? configureApp = null,
        string? usePathBase = null,
        string? serverPathBase = null,
        bool useExplicitRouting = true,
        bool addAuthorizationServices = true,
        bool map = true)
    {
        // The environment is pinned so that the ambient ASPNETCORE_ENVIRONMENT of the machine
        // running the tests cannot change the pipeline. In Development, WebApplication inserts the
        // developer exception page, which turns an exception that a test expects to observe into a
        // 500 response.
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseTestServer(options =>
        {
            options.BaseAddress = BaseAddress;
        });
        builder.Services.AddSingleton(LoggerFactory);

        if (addAuthorizationServices)
        {
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
        }

        if (serverPathBase is not null)
        {
            // Startup filters run before the pipeline, so this one stands in for a server that
            // populates the path base from a hosted virtual directory.
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

            if (useExplicitRouting)
            {
                // Routing has to be added explicitly here so that it runs after UsePathBase.
                app.UseRouting();
            }
        }

        // UseAuthentication and UseAuthorization are deliberately not called. WebApplication injects
        // both when the matching services are registered, and defers them until after routing when
        // routing is added explicitly, so they observe the matched endpoint. Calling them here would
        // set the flags that suppress that injection, moving responsibility for their placement into
        // this helper for no benefit.
        configureApp?.Invoke(app);

        if (map)
        {
            app.MapWellKnownPasskeyEndpoints();
        }

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
