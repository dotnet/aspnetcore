// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Authentication.Facebook;

public class FacebookTests : RemoteAuthenticationTests<FacebookOptions>
{
    protected override string DefaultScheme => FacebookDefaults.AuthenticationScheme;
    protected override Type HandlerType => typeof(FacebookHandler);
    protected override bool SupportsSignIn { get => false; }
    protected override bool SupportsSignOut { get => false; }

    protected override void RegisterAuth(AuthenticationBuilder services, Action<FacebookOptions> configure)
    {
        services.AddFacebook(o =>
        {
            ConfigureDefaults(o);
            configure.Invoke(o);
        });
    }

    protected override void ConfigureDefaults(FacebookOptions o)
    {
        o.AppId = "whatever";
        o.AppSecret = "PLACEHOLDER";
        o.SignInScheme = "auth1";
    }

    [Fact]
    public async Task ThrowsIfAppIdMissing()
    {
        using var host = await CreateHost(
            app => { },
            services => services.AddAuthentication().AddFacebook(o => o.SignInScheme = "PLACEHOLDER"),
            async context =>
            {
                await Assert.ThrowsAsync<ArgumentNullException>("AppId", () => context.ChallengeAsync("Facebook"));
                return true;
            });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task ThrowsIfAppSecretMissing()
    {
        using var host = await CreateHost(
            app => { },
            services => services.AddAuthentication().AddFacebook(o => o.AppId = "Whatever"),
            async context =>
            {
                await Assert.ThrowsAsync<ArgumentNullException>("AppSecret", () => context.ChallengeAsync("Facebook"));
                return true;
            });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task ChallengeWillTriggerApplyRedirectEvent()
    {
        using var host = await CreateHost(
            app =>
            {
                app.UseAuthentication();
            },
            services =>
            {
                services.AddAuthentication("External")
                    .AddCookie("External", o => { })
                    .AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                    o.Events = new OAuthEvents
                    {
                        OnRedirectToAuthorizationEndpoint = context =>
                        {
                            context.Response.Redirect(context.RedirectUri + "&custom=test");
                            return Task.FromResult(0);
                        }
                    };
                });
            },
            async context =>
            {
                await context.ChallengeAsync("Facebook");
                return true;
            });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var query = transaction.Response.Headers.Location.Query;
        Assert.Contains("custom=test", query);
    }

    [Fact]
    public async Task ChallengeWillIncludeScopeAsConfigured()
    {
        using var host = await CreateHost(
            app => app.UseAuthentication(),
            services =>
            {
                services.AddAuthentication(o => o.DisableAutoDefaultScheme = true).AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                    o.Scope.Clear();
                    o.Scope.Add("foo");
                    o.Scope.Add("bar");
                });
            },
            async context =>
            {
                await context.ChallengeAsync(FacebookDefaults.AuthenticationScheme);
                return true;
            });

        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("scope=foo,bar", res.Headers.Location.Query);
    }

    [Fact]
    public async Task ChallengeWillIncludeScopeAsOverwritten()
    {
        using var host = await CreateHost(
            app => app.UseAuthentication(),
            services =>
            {
                services.AddAuthentication(o => o.DisableAutoDefaultScheme = true).AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                    o.Scope.Clear();
                    o.Scope.Add("foo");
                    o.Scope.Add("bar");
                });
            },
            async context =>
            {
                var properties = new OAuthChallengeProperties();
                properties.SetScope("baz", "qux");
                await context.ChallengeAsync(FacebookDefaults.AuthenticationScheme, properties);
                return true;
            });

        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("scope=baz,qux", res.Headers.Location.Query);
    }

    [Fact]
    public async Task ChallengeWillIncludeScopeAsOverwrittenWithBaseAuthenticationProperties()
    {
        using var host = await CreateHost(
            app => app.UseAuthentication(),
            services =>
            {
                services.AddAuthentication(o => o.DisableAutoDefaultScheme = true).AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                    o.Scope.Clear();
                    o.Scope.Add("foo");
                    o.Scope.Add("bar");
                });
            },
            async context =>
            {
                var properties = new AuthenticationProperties();
                properties.SetParameter(OAuthChallengeProperties.ScopeKey, new string[] { "baz", "qux" });
                await context.ChallengeAsync(FacebookDefaults.AuthenticationScheme, properties);
                return true;
            });

        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        var res = transaction.Response;

        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("scope=baz,qux", res.Headers.Location.Query);
    }

    [Fact]
    public async Task NestedMapWillNotAffectRedirect()
    {
        using var host = await CreateHost(app => app.Map("/base", map =>
        {
            map.UseAuthentication();
            map.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
        }),
        services =>
        {
            services.AddAuthentication()
                .AddCookie("External", o => { })
                .AddFacebook(o =>
            {
                o.AppId = "Test App Id";
                o.AppSecret = "Test App Secret";
            });
        },
        handler: null);

        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/base/login");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location.AbsoluteUri;
        Assert.Contains("https://www.facebook.com/v14.0/dialog/oauth", location);
        Assert.Contains("response_type=code", location);
        Assert.Contains("client_id=", location);
        Assert.Contains("redirect_uri=" + UrlEncoder.Default.Encode("http://example.com/base/signin-facebook"), location);
        Assert.Contains("scope=", location);
        Assert.Contains("state=", location);
    }

    [Fact]
    public async Task MapWillNotAffectRedirect()
    {
        using var host = await CreateHost(
            app =>
            {
                app.UseAuthentication();
                app.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Facebook", new AuthenticationProperties() { RedirectUri = "/" })));
            },
            services =>
            {
                services.AddAuthentication()
                    .AddCookie("External", o => { })
                    .AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                    o.SignInScheme = "External";
                });
            },
            handler: null);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/login");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location.AbsoluteUri;
        Assert.Contains("https://www.facebook.com/v14.0/dialog/oauth", location);
        Assert.Contains("response_type=code", location);
        Assert.Contains("client_id=", location);
        Assert.Contains("redirect_uri=" + UrlEncoder.Default.Encode("http://example.com/signin-facebook"), location);
        Assert.Contains("scope=", location);
        Assert.Contains("state=", location);
    }

    [Fact]
    public async Task ChallengeWillTriggerRedirection()
    {
        using var host = await CreateHost(
            app => app.UseAuthentication(),
            services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultSignInScheme = "External";
                })
                    .AddCookie()
                    .AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                });
            },
            async context =>
            {
                await context.ChallengeAsync("Facebook");
                return true;
            });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location.AbsoluteUri;
        Assert.Contains("https://www.facebook.com/v14.0/dialog/oauth", location);
        Assert.Contains("response_type=code", location);
        Assert.Contains("client_id=", location);
        Assert.Contains("redirect_uri=", location);
        Assert.Contains("scope=", location);
        Assert.Contains("state=", location);
        Assert.Contains("code_challenge=", location);
        Assert.Contains("code_challenge_method=S256", location);
    }

    [Fact]
    public async Task CustomUserInfoEndpointHasValidGraphQuery()
    {
        var customUserInfoEndpoint = "https://graph.facebook.com/me?fields=email,timezone,picture";
        var finalUserInfoEndpoint = string.Empty;
        var stateFormat = new PropertiesDataFormat(new EphemeralDataProtectionProvider(NullLoggerFactory.Instance).CreateProtector("FacebookTest"));
        using var host = await CreateHost(
            app => app.UseAuthentication(),
            services =>
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie()
                    .AddFacebook(o =>
                {
                    o.AppId = "Test App Id";
                    o.AppSecret = "Test App Secret";
                    o.StateDataFormat = stateFormat;
                    o.UserInformationEndpoint = customUserInfoEndpoint;
                    o.BackchannelHttpHandler = new TestHttpMessageHandler
                    {
                        Sender = req =>
                        {
                            if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == FacebookDefaults.TokenEndpoint)
                            {
                                var res = new HttpResponseMessage(HttpStatusCode.OK);
                                var graphResponse = "{ \"access_token\": \"TestAuthToken\" }";
                                res.Content = new StringContent(graphResponse, Encoding.UTF8);
                                return res;
                            }
                            if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) ==
                                new Uri(customUserInfoEndpoint).GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped))
                            {
                                finalUserInfoEndpoint = req.RequestUri.ToString();
                                var res = new HttpResponseMessage(HttpStatusCode.OK);
                                var graphResponse = "{ \"id\": \"TestProfileId\", \"name\": \"TestName\" }";
                                res.Content = new StringContent(graphResponse, Encoding.UTF8);
                                return res;
                            }
                            return null;
                        }
                    };
                });
            },
            handler: null);

        var properties = new AuthenticationProperties();
        var correlationKey = ".xsrf";
        var correlationValue = "TestCorrelationId";
        properties.Items.Add(correlationKey, correlationValue);
        properties.RedirectUri = "/me";
        var state = stateFormat.Protect(properties);
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync(
            "https://example.com/signin-facebook?code=TestCode&state=" + UrlEncoder.Default.Encode(state),
            $".AspNetCore.Correlation.{correlationValue}=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/me", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(1, finalUserInfoEndpoint.Count(c => c == '?'));
        Assert.Contains("fields=email,timezone,picture", finalUserInfoEndpoint);
        Assert.Contains("&access_token=", finalUserInfoEndpoint);
        Assert.Contains("&appsecret_proof=b7fb6d5a4510926b4af6fe080497827d791dc45fe6541d88ba77bdf6e8e208c6&", finalUserInfoEndpoint);
    }

    [Fact]
    public async Task PkceSentToTokenEndpoint()
    {
        using var host = await CreateHost(
            app => app.UseAuthentication(),
            services =>
            {
                services.AddAuthentication(TestExtensions.CookieAuthenticationScheme)
                    .AddCookie(TestExtensions.CookieAuthenticationScheme)
                    .AddFacebook(o =>
                    {
                        o.AppId = "Test App Id";
                        o.AppSecret = "Test App Secret";
                        o.BackchannelHttpHandler = new TestHttpMessageHandler
                        {
                            Sender = req =>
                            {
                                if (req.RequestUri.AbsoluteUri == "https://graph.facebook.com/v14.0/oauth/access_token")
                                {
                                    var body = req.Content.ReadAsStringAsync().Result;
                                    var form = new FormReader(body);
                                    var entries = form.ReadForm();
                                    Assert.Equal("Test App Id", entries["client_id"]);
                                    Assert.Equal("https://example.com/signin-facebook", entries["redirect_uri"]);
                                    Assert.Equal("Test App Secret", entries["client_secret"]);
                                    Assert.Equal("TestCode", entries["code"]);
                                    Assert.Equal("authorization_code", entries["grant_type"]);
                                    Assert.False(string.IsNullOrEmpty(entries["code_verifier"]));

                                    return ReturnJsonResponse(new
                                    {
                                        access_token = "Test Access Token",
                                        expire_in = 3600,
                                        token_type = "Bearer",
                                    });
                                }
                                else if (req.RequestUri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped) == "https://graph.facebook.com/v14.0/me")
                                {
                                    return ReturnJsonResponse(new
                                    {
                                        id = "Test User ID",
                                        displayName = "Test Name",
                                        givenName = "Test Given Name",
                                        surname = "Test Family Name",
                                        mail = "Test email"
                                    });
                                }

                                return null;
                            }
                        };
                    });
            },
            async context =>
            {
                await context.ChallengeAsync("Facebook");
                return true;
            });
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("https://example.com/challenge");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var locationUri = transaction.Response.Headers.Location;
        Assert.StartsWith("https://www.facebook.com/v14.0/dialog/oauth", locationUri.AbsoluteUri);

        var queryParams = QueryHelpers.ParseQuery(locationUri.Query);
        Assert.False(string.IsNullOrEmpty(queryParams["code_challenge"]));
        Assert.Equal("S256", queryParams["code_challenge_method"]);

        var nonceCookie = transaction.SetCookie.Single();
        nonceCookie = nonceCookie.Substring(0, nonceCookie.IndexOf(';'));

        transaction = await server.SendAsync(
            "https://example.com/signin-facebook?code=TestCode&state=" + queryParams["state"],
            nonceCookie);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/challenge", transaction.Response.Headers.GetValues("Location").First());
        Assert.Equal(2, transaction.SetCookie.Count);
        Assert.StartsWith(".AspNetCore.Correlation.", transaction.SetCookie[0]);
        Assert.StartsWith(".AspNetCore." + TestExtensions.CookieAuthenticationScheme, transaction.SetCookie[1]);
    }

    private static async Task<IHost> CreateHost(Action<IApplicationBuilder> configure, Action<IServiceCollection> configureServices, Func<HttpContext, Task<bool>> handler)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        configure?.Invoke(app);
                        app.Use(async (context, next) =>
                        {
                            if (handler == null || !await handler(context))
                            {
                                await next(context);
                            }
                        });
                    })
                    .ConfigureServices(configureServices))
            .Build();

        await host.StartAsync();
        return host;
    }

    private static HttpResponseMessage ReturnJsonResponse(object content, HttpStatusCode code = HttpStatusCode.OK)
    {
        var res = new HttpResponseMessage(code);
        var text = Newtonsoft.Json.JsonConvert.SerializeObject(content);
        res.Content = new StringContent(text, Encoding.UTF8, "application/json");
        return res;
    }
}
