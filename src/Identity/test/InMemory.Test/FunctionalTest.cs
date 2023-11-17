// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Identity.InMemory;

public class FunctionalTest : LoggedTest
{
    const string TestPassword = "[PLACEHOLDER]-1a";

    [Fact]
    public async Task CanChangePasswordOptions()
    {
        var server = await CreateServer(services => services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = false;
        }));

        var transaction1 = await SendAsync(server, "http://example.com/createSimple");

        // Assert
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);
    }

    [Fact]
    public async Task CookieContainsRoleClaim()
    {
        var server = await CreateServer(null, null, null, testCore: true);

        var transaction1 = await SendAsync(server, "http://example.com/createMe");
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);

        var transaction2 = await SendAsync(server, "http://example.com/pwdLogin/false");
        Assert.Equal(HttpStatusCode.OK, transaction2.Response.StatusCode);
        Assert.NotNull(transaction2.SetCookie);
        Assert.DoesNotContain("; expires=", transaction2.SetCookie);

        var transaction3 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction3, ClaimTypes.Name));
        Assert.Equal("role", FindClaimValue(transaction3, ClaimTypes.Role));
        Assert.Null(transaction3.SetCookie);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanCreateMeLoginAndCookieStopsWorkingAfterExpiration(bool testCore)
    {
        var timeProvider = new FakeTimeProvider();
        var server = await CreateServer(services =>
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.TimeProvider = timeProvider;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            });
            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.TimeProvider = timeProvider;
            });
        }, testCore: testCore);

        var transaction1 = await SendAsync(server, "http://example.com/createMe");
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);

        var transaction2 = await SendAsync(server, "http://example.com/pwdLogin/false");
        Assert.Equal(HttpStatusCode.OK, transaction2.Response.StatusCode);
        Assert.NotNull(transaction2.SetCookie);
        Assert.DoesNotContain("; expires=", transaction2.SetCookie);

        var transaction3 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction3, ClaimTypes.Name));
        Assert.Null(transaction3.SetCookie);

        timeProvider.Advance(TimeSpan.FromMinutes(7));

        var transaction4 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction4, ClaimTypes.Name));
        Assert.Null(transaction4.SetCookie);

        timeProvider.Advance(TimeSpan.FromMinutes(7));

        var transaction5 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
        Assert.Null(transaction5.SetCookie);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CanCreateMeLoginAndSecurityStampExtendsExpiration(bool rememberMe, bool testCore)
    {
        var timeProvider = new FakeTimeProvider();
        var server = await CreateServer(services =>
        {
            services.AddSingleton<TimeProvider>(timeProvider);
        }, testCore: testCore);

        var transaction1 = await SendAsync(server, "http://example.com/createMe");
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);

        var transaction2 = await SendAsync(server, "http://example.com/pwdLogin/" + rememberMe);
        Assert.Equal(HttpStatusCode.OK, transaction2.Response.StatusCode);
        Assert.NotNull(transaction2.SetCookie);
        if (rememberMe)
        {
            Assert.Contains("; expires=", transaction2.SetCookie);
        }
        else
        {
            Assert.DoesNotContain("; expires=", transaction2.SetCookie);
        }

        var transaction3 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction3, ClaimTypes.Name));
        Assert.Null(transaction3.SetCookie);

        // Make sure we don't get a new cookie yet
        timeProvider.Advance(TimeSpan.FromMinutes(10));
        var transaction4 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction4, ClaimTypes.Name));
        Assert.Null(transaction4.SetCookie);

        // Go past SecurityStampValidation interval and ensure we get a new cookie
        timeProvider.Advance(TimeSpan.FromMinutes(21));

        var transaction5 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.NotNull(transaction5.SetCookie);
        Assert.Equal("hao", FindClaimValue(transaction5, ClaimTypes.Name));

        // Make sure new cookie is valid
        var transaction6 = await SendAsync(server, "http://example.com/me", transaction5.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction6, ClaimTypes.Name));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanAccessOldPrincipalDuringSecurityStampReplacement(bool testCore)
    {
        var timeProvider = new FakeTimeProvider();
        var server = await CreateServer(services =>
        {
            services.AddSingleton<TimeProvider>(timeProvider);
            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                options.OnRefreshingPrincipal = c =>
                {
                    var newId = new ClaimsIdentity();
                    newId.AddClaim(new Claim("PreviousName", c.CurrentPrincipal.Identity.Name));
                    c.NewPrincipal.AddIdentity(newId);
                    return Task.FromResult(0);
                };
            });
        }, testCore: testCore);

        var transaction1 = await SendAsync(server, "http://example.com/createMe");
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);

        var transaction2 = await SendAsync(server, "http://example.com/pwdLogin/false");
        Assert.Equal(HttpStatusCode.OK, transaction2.Response.StatusCode);
        Assert.NotNull(transaction2.SetCookie);
        Assert.DoesNotContain("; expires=", transaction2.SetCookie);

        var transaction3 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction3, ClaimTypes.Name));
        Assert.Null(transaction3.SetCookie);

        // Make sure we don't get a new cookie yet
        timeProvider.Advance(TimeSpan.FromMinutes(10));
        var transaction4 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction4, ClaimTypes.Name));
        Assert.Null(transaction4.SetCookie);

        // Go past SecurityStampValidation interval and ensure we get a new cookie
        timeProvider.Advance(TimeSpan.FromMinutes(21));

        var transaction5 = await SendAsync(server, "http://example.com/me", transaction2.CookieNameValue);
        Assert.NotNull(transaction5.SetCookie);
        Assert.Equal("hao", FindClaimValue(transaction5, ClaimTypes.Name));
        Assert.Equal("hao", FindClaimValue(transaction5, "PreviousName"));

        // Make sure new cookie is valid
        var transaction6 = await SendAsync(server, "http://example.com/me", transaction5.CookieNameValue);
        Assert.Equal("hao", FindClaimValue(transaction6, ClaimTypes.Name));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TwoFactorRememberCookieVerification(bool testCore)
    {
        var timeProvider = new FakeTimeProvider();
        var server = await CreateServer(services => services.AddSingleton<TimeProvider>(timeProvider), testCore: testCore);

        var transaction1 = await SendAsync(server, "http://example.com/createMe");
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);

        var transaction2 = await SendAsync(server, "http://example.com/twofactorRememeber");
        Assert.Equal(HttpStatusCode.OK, transaction2.Response.StatusCode);

        var setCookie = transaction2.SetCookie;
        Assert.Contains(IdentityConstants.TwoFactorRememberMeScheme + "=", setCookie);
        Assert.Contains("; expires=", setCookie);

        var transaction3 = await SendAsync(server, "http://example.com/isTwoFactorRememebered", transaction2.CookieNameValue);
        Assert.Equal(HttpStatusCode.OK, transaction3.Response.StatusCode);

        // Wait for validation interval
        timeProvider.Advance(TimeSpan.FromMinutes(30));

        var transaction4 = await SendAsync(server, "http://example.com/isTwoFactorRememebered", transaction2.CookieNameValue);
        Assert.Equal(HttpStatusCode.OK, transaction4.Response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TwoFactorRememberCookieClearedBySecurityStampChange(bool testCore)
    {
        var timeProvider = new FakeTimeProvider();
        var server = await CreateServer(services => services.AddSingleton<TimeProvider>(timeProvider), testCore: testCore);

        var transaction1 = await SendAsync(server, "http://example.com/createMe");
        Assert.Equal(HttpStatusCode.OK, transaction1.Response.StatusCode);
        Assert.Null(transaction1.SetCookie);

        var transaction2 = await SendAsync(server, "http://example.com/twofactorRememeber");
        Assert.Equal(HttpStatusCode.OK, transaction2.Response.StatusCode);

        var setCookie = transaction2.SetCookie;
        Assert.Contains(IdentityConstants.TwoFactorRememberMeScheme + "=", setCookie);
        Assert.Contains("; expires=", setCookie);

        var transaction3 = await SendAsync(server, "http://example.com/isTwoFactorRememebered", transaction2.CookieNameValue);
        Assert.Equal(HttpStatusCode.OK, transaction3.Response.StatusCode);

        var transaction4 = await SendAsync(server, "http://example.com/signoutEverywhere", transaction2.CookieNameValue);
        Assert.Equal(HttpStatusCode.OK, transaction4.Response.StatusCode);

        // Doesn't validate until after interval has passed
        var transaction5 = await SendAsync(server, "http://example.com/isTwoFactorRememebered", transaction2.CookieNameValue);
        Assert.Equal(HttpStatusCode.OK, transaction5.Response.StatusCode);

        // Wait for validation interval
        timeProvider.Advance(TimeSpan.FromMinutes(30) + TimeSpan.FromMilliseconds(1));

        var transaction6 = await SendAsync(server, "http://example.com/isTwoFactorRememebered", transaction2.CookieNameValue);
        Assert.Equal(HttpStatusCode.InternalServerError, transaction6.Response.StatusCode);
    }

    private static string FindClaimValue(Transaction transaction, string claimType)
    {
        var claim = transaction.ResponseElement.Elements("claim").SingleOrDefault(elt => elt.Attribute("type").Value == claimType);
        if (claim == null)
        {
            return null;
        }
        return claim.Attribute("value").Value;
    }

    private async Task<TestServer> CreateServer(Action<IServiceCollection> configureServices = null, Func<HttpContext, Task> testpath = null, Uri baseAddress = null, bool testCore = false)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;
                        var userManager = context.RequestServices.GetRequiredService<UserManager<PocoUser>>();
                        var roleManager = context.RequestServices.GetRequiredService<RoleManager<PocoRole>>();
                        var signInManager = context.RequestServices.GetRequiredService<SignInManager<PocoUser>>();
                        PathString remainder;
                        if (req.Path == new PathString("/normal"))
                        {
                            res.StatusCode = 200;
                        }
                        else if (req.Path == new PathString("/createMe"))
                        {
                            var user = new PocoUser("hao");
                            var result = await userManager.CreateAsync(user, TestPassword);
                            if (result.Succeeded)
                            {
                                result = await roleManager.CreateAsync(new PocoRole("role"));
                            }
                            if (result.Succeeded)
                            {
                                result = await userManager.AddToRoleAsync(user, "role");
                            }
                            res.StatusCode = result.Succeeded ? 200 : 500;
                        }
                        else if (req.Path == new PathString("/createSimple"))
                        {
                            var result = await userManager.CreateAsync(new PocoUser("simple"), "aaaaaa");
                            res.StatusCode = result.Succeeded ? 200 : 500;
                        }
                        else if (req.Path == new PathString("/signoutEverywhere"))
                        {
                            var user = await userManager.FindByNameAsync("hao");
                            var result = await userManager.UpdateSecurityStampAsync(user);
                            res.StatusCode = result.Succeeded ? 200 : 500;
                        }
                        else if (req.Path.StartsWithSegments(new PathString("/pwdLogin"), out remainder))
                        {
                            var isPersistent = bool.Parse(remainder.Value.AsSpan(1));
                            var result = await signInManager.PasswordSignInAsync("hao", TestPassword, isPersistent, false);
                            res.StatusCode = result.Succeeded ? 200 : 500;
                        }
                        else if (req.Path == new PathString("/twofactorRememeber"))
                        {
                            var user = await userManager.FindByNameAsync("hao");
                            await signInManager.RememberTwoFactorClientAsync(user);
                            res.StatusCode = 200;
                        }
                        else if (req.Path == new PathString("/isTwoFactorRememebered"))
                        {
                            var user = await userManager.FindByNameAsync("hao");
                            var result = await signInManager.IsTwoFactorClientRememberedAsync(user);
                            res.StatusCode = result ? 200 : 500;
                        }
                        else if (req.Path == new PathString("/hasTwoFactorUserId"))
                        {
                            var result = await context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
                            res.StatusCode = result.Succeeded ? 200 : 500;
                        }
                        else if (req.Path == new PathString("/me"))
                        {
                            await DescribeAsync(res, AuthenticateResult.Success(new AuthenticationTicket(context.User, null, "Application")));
                        }
                        else if (req.Path.StartsWithSegments(new PathString("/me"), out remainder))
                        {
                            var auth = await context.AuthenticateAsync(remainder.Value.Substring(1));
                            await DescribeAsync(res, auth);
                        }
                        else if (req.Path == new PathString("/testpath") && testpath != null)
                        {
                            await testpath(context);
                        }
                        else
                        {
                            await next(context);
                        }
                    });
                })
                .ConfigureServices(services =>
                {
                    if (testCore)
                    {
                        services.AddIdentityCore<PocoUser>()
                            .AddRoles<PocoRole>()
                            .AddSignInManager()
                            .AddDefaultTokenProviders();
                        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
                    }
                    else
                    {
                        services.AddIdentity<PocoUser, PocoRole>().AddDefaultTokenProviders();
                    }
                    var store = new InMemoryStore<PocoUser, PocoRole>();
                    services.AddSingleton<IUserStore<PocoUser>>(store);
                    services.AddSingleton<IRoleStore<PocoRole>>(store);
                    configureServices?.Invoke(services);
                    AddTestLogging(services);
                })
                .UseTestServer())
            .Build();
        await host.StartAsync();
        var server = host.GetTestServer();
        server.BaseAddress = baseAddress;
        return server;
    }

    private static async Task DescribeAsync(HttpResponse res, AuthenticateResult result)
    {
        res.StatusCode = 200;
        res.ContentType = "text/xml";
        var xml = new XElement("xml");
        if (result != null && result.Principal != null)
        {
            xml.Add(result.Principal.Claims.Select(claim => new XElement("claim", new XAttribute("type", claim.Type), new XAttribute("value", claim.Value))));
        }
        if (result != null && result.Properties != null)
        {
            xml.Add(result.Properties.Items.Select(extra => new XElement("extra", new XAttribute("type", extra.Key), new XAttribute("value", extra.Value))));
        }
        using (var memory = new MemoryStream())
        {
            using (var writer = XmlWriter.Create(memory, new XmlWriterSettings { Encoding = Encoding.UTF8 }))
            {
                xml.WriteTo(writer);
            }
            await res.Body.WriteAsync(memory.ToArray(), 0, memory.ToArray().Length);
        }
    }

    private static async Task<Transaction> SendAsync(TestServer server, string uri, string cookieHeader = null, bool ajaxRequest = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }
        if (ajaxRequest)
        {
            request.Headers.Add(HeaderNames.XRequestedWith, "XMLHttpRequest");
        }
        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };
        if (transaction.Response.Headers.Contains("Set-Cookie"))
        {
            transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        }
        if (!string.IsNullOrEmpty(transaction.SetCookie))
        {
            transaction.CookieNameValue = transaction.SetCookie.Split(new[] { ';' }, 2).First();
        }
        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        if (transaction.Response.Content != null &&
            transaction.Response.Content.Headers.ContentType != null &&
            transaction.Response.Content.Headers.ContentType.MediaType == "text/xml")
        {
            transaction.ResponseElement = XElement.Parse(transaction.ResponseText);
        }
        return transaction;
    }

    private class Transaction
    {
        public HttpRequestMessage Request { get; set; }
        public HttpResponseMessage Response { get; set; }

        public string SetCookie { get; set; }
        public string CookieNameValue { get; set; }

        public string ResponseText { get; set; }
        public XElement ResponseElement { get; set; }
    }
}
