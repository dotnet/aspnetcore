// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.CookiePolicy.Test;

public class CookieConsentTests
{
    [Fact]
    public async Task ConsentChecksOffByDefault()
    {
        var httpContext = await RunTestAsync(options => { }, requestContext => { }, context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.False(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.True(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });
        Assert.Equal("Test=Value; path=/", httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task ConsentEnabledForTemplateScenario()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { }, context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });
        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task NonEssentialCookiesWithOptionsExcluded()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { }, context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value", new CookieOptions() { IsEssential = false });
            return Task.CompletedTask;
        });
        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task NonEssentialCookiesCanBeAllowedViaOnAppendCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.OnAppendCookie = context =>
            {
                Assert.True(context.IsConsentNeeded);
                Assert.False(context.HasConsent);
                Assert.False(context.IssueCookie);
                context.IssueCookie = true;
            };
        },
        requestContext => { }, context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value", new CookieOptions() { IsEssential = false });
            return Task.CompletedTask;
        });
        Assert.Equal("Test=Value; path=/", httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task NeedsConsentDoesNotPreventEssentialCookies()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { }, context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value", new CookieOptions() { IsEssential = true });
            return Task.CompletedTask;
        });
        Assert.Equal("Test=Value; path=/", httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task EssentialCookiesCanBeExcludedByOnAppendCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.OnAppendCookie = context =>
            {
                Assert.True(context.IsConsentNeeded);
                Assert.True(context.HasConsent);
                Assert.True(context.IssueCookie);
                context.IssueCookie = false;
            };
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=yes";
        },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value", new CookieOptions() { IsEssential = true });
            return Task.CompletedTask;
        });
        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task HasConsentReadsRequestCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=yes";
        },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });
        Assert.Equal("Test=Value; path=/", httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task HasConsentIgnoresInvalidRequestCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=IAmATeapot";
        },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });
        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task GrantConsentSetsCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(2, cookies.Count);
        var consentCookie = cookies[0];
        Assert.Equal(".AspNet.Consent", consentCookie.Name);
        Assert.Equal("yes", consentCookie.Value);
        Assert.True(consentCookie.Expires.HasValue);
        Assert.True(consentCookie.Expires.Value > DateTimeOffset.Now + TimeSpan.FromDays(364));
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);
        var testCookie = cookies[1];
        Assert.Equal("Test", testCookie.Name);
        Assert.Equal("Value", testCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, testCookie.SameSite);
        Assert.Null(testCookie.Expires);
    }

    [Fact]
    public async Task GrantConsentAppliesPolicyToConsentCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = Http.SameSiteMode.Strict;
            options.OnAppendCookie = context =>
            {
                Assert.Equal(".AspNet.Consent", context.CookieName);
                Assert.Equal("yes", context.CookieValue);
                Assert.Equal(Http.SameSiteMode.Strict, context.CookieOptions.SameSite);
                context.CookieName += "1";
                context.CookieValue += "1";
                context.CookieOptions.Extensions.Add("extension");
            };
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(1, cookies.Count);
        var consentCookie = cookies[0];
        Assert.Equal(".AspNet.Consent1", consentCookie.Name);
        Assert.Equal("yes1", consentCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Strict, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);
        Assert.Contains("extension", consentCookie.Extensions);
    }

    [Fact]
    public async Task GrantConsentWhenAlreadyHasItDoesNotSetCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=yes";
        },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });

        Assert.Equal("Test=Value; path=/", httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task GrantConsentAfterResponseStartsSetsHasConsentButDoesNotSetCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { },
        async context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            await context.Response.WriteAsync("Started.");

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            Assert.Throws<InvalidOperationException>(() => context.Response.Cookies.Append("Test", "Value"));

            await context.Response.WriteAsync("Granted.");
        });

        var reader = new StreamReader(httpContext.Response.Body);
        Assert.Equal("Started.Granted.", await reader.ReadToEndAsync());
        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task WithdrawConsentWhenNotHasConsentNoOps()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            feature.WithdrawConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            context.Response.Cookies.Append("Test", "Value");
            return Task.CompletedTask;
        });

        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task WithdrawConsentDeletesCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=yes";
        },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value1");

            feature.WithdrawConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            context.Response.Cookies.Append("Test", "Value2");
            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(2, cookies.Count);
        var testCookie = cookies[0];
        Assert.Equal("Test", testCookie.Name);
        Assert.Equal("Value1", testCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, testCookie.SameSite);
        Assert.Null(testCookie.Expires);
        var consentCookie = cookies[1];
        Assert.Equal(".AspNet.Consent", consentCookie.Name);
        Assert.Equal("", consentCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);
    }

    [Fact]
    public async Task WithdrawConsentAppliesPolicyToDeleteCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = Http.SameSiteMode.Strict;
            options.OnDeleteCookie = context =>
            {
                Assert.Equal(".AspNet.Consent", context.CookieName);
                context.CookieName += "1";
            };
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=yes";
        },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            feature.WithdrawConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(1, cookies.Count);
        var consentCookie = cookies[0];
        Assert.Equal(".AspNet.Consent1", consentCookie.Name);
        Assert.Equal("", consentCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Strict, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);
    }

    [Fact]
    public async Task WithdrawConsentAfterResponseHasStartedDoesNotDeleteCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext =>
        {
            requestContext.Request.Headers.Cookie = ".AspNet.Consent=yes";
        },
        async context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);
            context.Response.Cookies.Append("Test", "Value1");

            await context.Response.WriteAsync("Started.");

            feature.WithdrawConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            // Doesn't throw the normal InvalidOperationException because the cookie is never written
            context.Response.Cookies.Append("Test", "Value2");

            await context.Response.WriteAsync("Withdrawn.");
        });

        var reader = new StreamReader(httpContext.Response.Body);
        Assert.Equal("Started.Withdrawn.", await reader.ReadToEndAsync());
        Assert.Equal("Test=Value1; path=/", httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task DeleteCookieDoesNotRequireConsent()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Delete("Test");
            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(1, cookies.Count);
        var testCookie = cookies[0];
        Assert.Equal("Test", testCookie.Name);
        Assert.Equal("", testCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, testCookie.SameSite);
        Assert.NotNull(testCookie.Expires);
    }

    [Fact]
    public async Task OnDeleteCookieCanSuppressCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.OnDeleteCookie = context =>
            {
                Assert.True(context.IsConsentNeeded);
                Assert.False(context.HasConsent);
                Assert.True(context.IssueCookie);
                context.IssueCookie = false;
            };
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);
            context.Response.Cookies.Delete("Test");
            return Task.CompletedTask;
        });

        Assert.Empty(httpContext.Response.Headers.SetCookie);
    }

    [Fact]
    public async Task CreateConsentCookieMatchesGrantConsentCookie()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            var cookie = feature.CreateConsentCookie();
            context.Response.Headers["ManualCookie"] = cookie;

            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(1, cookies.Count);
        var consentCookie = cookies[0];
        Assert.Equal(".AspNet.Consent", consentCookie.Name);
        Assert.Equal("yes", consentCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);

        cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers["ManualCookie"]);
        Assert.Equal(1, cookies.Count);
        var manualCookie = cookies[0];
        Assert.Equal(consentCookie.Name, manualCookie.Name);
        Assert.Equal(consentCookie.Value, manualCookie.Value);
        Assert.Equal(consentCookie.SameSite, manualCookie.SameSite);
        Assert.NotNull(manualCookie.Expires); // Expires may not exactly match to the second.
    }

    [Fact]
    public async Task CreateConsentCookieAppliesPolicy()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.MinimumSameSitePolicy = Http.SameSiteMode.Strict;
            options.OnAppendCookie = context =>
            {
                Assert.Equal(".AspNet.Consent", context.CookieName);
                Assert.Equal("yes", context.CookieValue);
                Assert.Equal(Http.SameSiteMode.Strict, context.CookieOptions.SameSite);
                context.CookieName += "1";
                context.CookieValue += "1";
            };
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            var cookie = feature.CreateConsentCookie();
            context.Response.Headers["ManualCookie"] = cookie;

            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(1, cookies.Count);
        var consentCookie = cookies[0];
        Assert.Equal(".AspNet.Consent1", consentCookie.Name);
        Assert.Equal("yes1", consentCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Strict, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);

        cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers["ManualCookie"]);
        Assert.Equal(1, cookies.Count);
        var manualCookie = cookies[0];
        Assert.Equal(consentCookie.Name, manualCookie.Name);
        Assert.Equal(consentCookie.Value, manualCookie.Value);
        Assert.Equal(consentCookie.SameSite, manualCookie.SameSite);
        Assert.NotNull(manualCookie.Expires); // Expires may not exactly match to the second.
    }

    [Fact]
    public async Task CreateConsentCookieMatchesGrantConsentCookieWhenCookieValueIsCustom()
    {
        var httpContext = await RunTestAsync(options =>
        {
            options.CheckConsentNeeded = context => true;
            options.ConsentCookieValue = "true";
        },
        requestContext => { },
        context =>
        {
            var feature = context.Features.Get<ITrackingConsentFeature>();
            Assert.True(feature.IsConsentNeeded);
            Assert.False(feature.HasConsent);
            Assert.False(feature.CanTrack);

            feature.GrantConsent();

            Assert.True(feature.IsConsentNeeded);
            Assert.True(feature.HasConsent);
            Assert.True(feature.CanTrack);

            var cookie = feature.CreateConsentCookie();
            context.Response.Headers["ManualCookie"] = cookie;

            return Task.CompletedTask;
        });

        var cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers.SetCookie);
        Assert.Equal(1, cookies.Count);
        var consentCookie = cookies[0];
        Assert.Equal(".AspNet.Consent", consentCookie.Name);
        Assert.Equal("true", consentCookie.Value);
        Assert.Equal(Net.Http.Headers.SameSiteMode.Unspecified, consentCookie.SameSite);
        Assert.NotNull(consentCookie.Expires);

        cookies = SetCookieHeaderValue.ParseList(httpContext.Response.Headers["ManualCookie"]);
        Assert.Equal(1, cookies.Count);
        var manualCookie = cookies[0];
        Assert.Equal(consentCookie.Name, manualCookie.Name);
        Assert.Equal(consentCookie.Value, manualCookie.Value);
        Assert.Equal(consentCookie.SameSite, manualCookie.SameSite);
        Assert.NotNull(manualCookie.Expires); // Expires may not exactly match to the second.
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void CreateCookiePolicyOptionsWithEmptyConsentCookieValueThrows(string value, string expectedMessage)
    {
        var options = new CookiePolicyOptions();

        ExceptionAssert.ThrowsArgument(
            () => options.ConsentCookieValue = value,
            "value",
            expectedMessage);
    }

    private async Task<HttpContext> RunTestAsync(Action<CookiePolicyOptions> configureOptions, Action<HttpContext> configureRequest, RequestDelegate handleRequest)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseCookiePolicy();
                        app.Run(handleRequest);
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.Configure(configureOptions);
            })
            .Build();

        var server = host.GetTestServer();

        await host.StartAsync();

        return await server.SendAsync(configureRequest);
    }
}
