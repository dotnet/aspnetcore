// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscRegistrationHeaderTests
{
    private const string SourceScheme = "Source";

    [Fact]
    public async Task WriteRegistration_FromEndpoint_WritesParseableRegistrationHeader()
    {
        using var host = await CreateRegistrationHostAsync();

        using var response = await host.GetTestClient().GetAsync("/direct");

        var header = Assert.Single(response.Headers.GetValues(DbscConstants.Headers.Registration));
        var challenge = AssertCompleteHeader(header, DbscDefaults.RegistrationPath);
        var challengeProtector = host.Services.GetRequiredService<DbscChallengeProtector>();
        Assert.True(challengeProtector.TryValidateRegistrationChallenge(challenge, Principal("alice")));
    }

    [Fact]
    public async Task WriteRegistration_WithUnknownScheme_ThrowsClearError()
    {
        using var host = await CreateRegistrationHostAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => host.GetTestClient().GetAsync("/unknown"));

        Assert.Equal(
            "'Unknown' is not a registered DBSC scheme. Call AddDbsc(\"Unknown\") to register it first.",
            exception.Message);
    }

    [Fact]
    public async Task WriteRegistration_HeaderMatchesAutomaticSignInHeader()
    {
        using var host = await CreateRegistrationHostAsync();
        var client = host.GetTestClient();

        using var directResponse = await client.GetAsync("/direct");
        using var automaticResponse = await client.GetAsync("/automatic");

        var directHeader = Assert.Single(directResponse.Headers.GetValues(DbscConstants.Headers.Registration));
        var automaticHeader = Assert.Single(automaticResponse.Headers.GetValues(DbscConstants.Headers.Registration));
        var directChallenge = AssertCompleteHeader(directHeader, DbscDefaults.RegistrationPath);
        var automaticChallenge = AssertCompleteHeader(automaticHeader, DbscDefaults.RegistrationPath);
        Assert.Equal(
            directHeader.Replace(directChallenge, "{challenge}", StringComparison.Ordinal),
            automaticHeader.Replace(automaticChallenge, "{challenge}", StringComparison.Ordinal));

        var challengeProtector = host.Services.GetRequiredService<DbscChallengeProtector>();
        var principal = Principal("alice");
        Assert.True(challengeProtector.TryValidateRegistrationChallenge(directChallenge, principal));
        Assert.True(challengeProtector.TryValidateRegistrationChallenge(automaticChallenge, principal));
    }

    [Fact]
    public async Task Emit_DefaultOptions_WritesExpectedRegistrationHeader()
    {
        using var harness = new EmitTestHarness(
            DbscDefaults.AuthenticationScheme,
            DbscDefaults.RegistrationPath);

        await harness.Emit();

        var header = AssertSingleHeader(harness.HttpContext);
        _ = AssertCompleteHeader(header, DbscDefaults.RegistrationPath);
    }

    [Fact]
    public async Task Emit_UsesNamedOptionsAndRequestPathBase()
    {
        const string scheme = "Custom";
        using var harness = new EmitTestHarness(
            scheme,
            "/custom/dbsc/register",
            "/tenant",
            ("Other", "/other/dbsc/register"));

        await harness.Emit();

        var header = AssertSingleHeader(harness.HttpContext);
        _ = AssertCompleteHeader(header, "/tenant/custom/dbsc/register");
        Assert.DoesNotContain("/other/dbsc/register", header);
        Assert.DoesNotContain("/tenant/tenant/", header);
    }

    [Theory]
    [InlineData("", "/quoted\"\\path", "/quoted%22%5Cpath")]
    [InlineData("", "/space path/café/雪", "/space%20path/caf%C3%A9/%E9%9B%AA")]
    [InlineData("/base path/café", "/register", "/base%20path/caf%C3%A9/register")]
    [InlineData("", "/register\r\nX-Injected: yes\"\\tail", "/register%0D%0AX-Injected:%20yes%22%5Ctail")]
    public async Task Emit_PathComponentsAreUriEncodedBeforeWritingHeader(
        string pathBase,
        string registrationPath,
        string expectedPath)
    {
        using var harness = new EmitTestHarness(
            DbscDefaults.AuthenticationScheme,
            registrationPath,
            pathBase);

        await harness.Emit();

        var header = AssertSingleHeader(harness.HttpContext);
        _ = AssertCompleteHeader(header, expectedPath);
        Assert.Single(Regex.Matches(header, ";path="));
        Assert.Single(Regex.Matches(header, ";challenge="));
        Assert.Equal(4, header.Count(character => character == '"'));
        Assert.DoesNotContain('\\', header);
        Assert.DoesNotContain('\r', header);
        Assert.DoesNotContain('\n', header);
        Assert.False(harness.HttpContext.Response.Headers.ContainsKey("X-Injected"));
    }

    [Fact]
    public async Task Emit_ChallengeIsBoundToSuppliedPrincipal()
    {
        var alice = Principal("alice");
        using var harness = new EmitTestHarness(
            DbscDefaults.AuthenticationScheme,
            DbscDefaults.RegistrationPath);

        await harness.Emit(alice);

        var header = AssertSingleHeader(harness.HttpContext);
        var challenge = AssertCompleteHeader(header, DbscDefaults.RegistrationPath);
        Assert.True(harness.ChallengeProtector.TryValidateRegistrationChallenge(challenge, alice));
        Assert.False(harness.ChallengeProtector.TryValidateRegistrationChallenge(challenge, Principal("bob")));
    }

    [Fact]
    public async Task Emit_Twice_AppendsTwoRegistrationHeaderValues()
    {
        using var harness = new EmitTestHarness(
            DbscDefaults.AuthenticationScheme,
            DbscDefaults.RegistrationPath);

        await harness.Emit();
        await harness.Emit();

        var values = harness.HttpContext.Response.Headers[DbscConstants.Headers.Registration];
        Assert.Equal(2, values.Count);
        Assert.NotNull(values[0]);
        Assert.NotNull(values[1]);
        var firstHeader = values[0]!;
        var secondHeader = values[1]!;
        var firstChallenge = AssertCompleteHeader(firstHeader, DbscDefaults.RegistrationPath);
        var secondChallenge = AssertCompleteHeader(secondHeader, DbscDefaults.RegistrationPath);
        Assert.NotEqual(firstChallenge, secondChallenge);
    }

    [Fact]
    public async Task Emit_AfterResponseStarted_ThrowsInvalidOperationException()
    {
        const string scheme = "ResponseStarted";
        var middlewareCompleted = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => ConfigureServices(
                        services,
                        scheme,
                        DbscDefaults.RegistrationPath))
                    .Configure(app => app.Run(async context =>
                    {
                        try
                        {
                            var challengeProtector = context.RequestServices
                                .GetRequiredService<DbscChallengeProtector>();
                            await context.Response.StartAsync();
                            Assert.True(context.Response.HasStarted);

                            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                                DbscRegistrationHeader.Emit(context, new ClaimsPrincipal(), properties: null, scheme));
                            Assert.Same(
                                challengeProtector,
                                context.RequestServices.GetRequiredService<DbscChallengeProtector>());
                            middlewareCompleted.SetResult(null);
                        }
                        catch (Exception exception)
                        {
                            middlewareCompleted.TrySetResult(exception);
                        }
                    }));
            })
            .Build();

        await host.StartAsync();
        _ = host.Services.GetRequiredService<DbscChallengeProtector>();
        using var response = await host.GetTestClient().GetAsync("/");
        var middlewareException = await middlewareCompleted.Task;
        if (middlewareException is not null)
        {
            ExceptionDispatchInfo.Capture(middlewareException).Throw();
        }
    }

    private static ClaimsPrincipal Principal(string subject)
        => new(new ClaimsIdentity([new Claim("sub", subject)], authenticationType: "Test"));

    private static async Task<IHost> CreateRegistrationHostAsync()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddDataProtection();
                        services.AddAuthentication(SourceScheme)
                            .AddCookie(SourceScheme)
                            .AddDbsc(
                                DbscDefaults.AuthenticationScheme,
                                options => options.SourceScheme = SourceScheme);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/direct", async context =>
                            {
                                context.User = Principal("alice");
                                await DbscRegistration.WriteRegistrationAsync(
                                    context,
                                    DbscDefaults.AuthenticationScheme);
                            });
                            endpoints.MapGet("/automatic", context =>
                                context.SignInAsync(SourceScheme, Principal("alice")));
                            endpoints.MapGet("/unknown", async context =>
                            {
                                context.User = Principal("alice");
                                await DbscRegistration.WriteRegistrationAsync(context, "Unknown");
                            });
                        });
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static string AssertSingleHeader(HttpContext httpContext)
    {
        var values = httpContext.Response.Headers[DbscConstants.Headers.Registration];
        Assert.Equal(1, values.Count);
        Assert.NotNull(values[0]);

        return values[0]!;
    }

    private static string AssertCompleteHeader(string header, string expectedPath)
    {
        var pattern = $"^{Regex.Escape(DbscConstants.AdvertisedAlgorithms)};path=\"{Regex.Escape(expectedPath)}\";challenge=\"(?<challenge>[A-Za-z0-9_-]+)\"$";
        var match = Regex.Match(header, pattern);
        Assert.True(match.Success, $"The registration header '{header}' did not match '{pattern}'.");

        return match.Groups["challenge"].Value;
    }

    private static void ConfigureServices(
        IServiceCollection services,
        string scheme,
        string registrationPath,
        params (string Scheme, string RegistrationPath)[] additionalOptions)
    {
        services.AddLogging();
        services.AddOptions();
        services.AddDataProtection();
        services.AddAuthentication()
            .AddScheme<DbscOptions, DbscHandler>(scheme, _ => { });
        services.AddOptions<DbscOptions>(scheme)
            .Configure(options => options.RegistrationPath = registrationPath);
        foreach (var (additionalScheme, additionalRegistrationPath) in additionalOptions)
        {
            services.AddOptions<DbscOptions>(additionalScheme)
                .Configure(options => options.RegistrationPath = additionalRegistrationPath);
        }
        services.AddSingleton<DbscChallengeProtector>();
    }

    private sealed class EmitTestHarness : IDisposable
    {
        private readonly string _scheme;
        private readonly ServiceProvider _serviceProvider;

        public EmitTestHarness(
            string scheme,
            string registrationPath,
            string pathBase = "",
            params (string Scheme, string RegistrationPath)[] additionalOptions)
        {
            _scheme = scheme;
            var services = new ServiceCollection();
            ConfigureServices(services, scheme, registrationPath, additionalOptions);
            _serviceProvider = services.BuildServiceProvider();
            ChallengeProtector = _serviceProvider.GetRequiredService<DbscChallengeProtector>();
            HttpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            HttpContext.Request.PathBase = pathBase;
        }

        public DbscChallengeProtector ChallengeProtector { get; }

        public HttpContext HttpContext { get; }

        public Task Emit(ClaimsPrincipal? principal = null)
            => DbscRegistrationHeader.Emit(HttpContext, principal, properties: null, _scheme);

        public void Dispose() => _serviceProvider.Dispose();
    }
}
