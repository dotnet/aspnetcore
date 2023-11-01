// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Microsoft.AspNetCore.Authentication.Cookies;

public class CookieTests : SharedAuthenticationTests<CookieAuthenticationOptions>
{
    private readonly FakeTimeProvider _timeProvider = new();

    protected override string DefaultScheme => CookieAuthenticationDefaults.AuthenticationScheme;
    protected override Type HandlerType => typeof(CookieAuthenticationHandler);

    protected override void RegisterAuth(AuthenticationBuilder services, Action<CookieAuthenticationOptions> configure)
    {
        services.AddCookie(configure);
    }

    [Fact]
    public async Task NormalRequestPassesThrough()
    {
        using var host = await CreateHost(s => { });
        using var server = host.GetTestServer();
        var response = await server.CreateClient().GetAsync("http://example.com/normal");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AjaxLoginRedirectToReturnUrlTurnsInto200WithLocationHeader()
    {
        using var host = await CreateHost(o => o.LoginPath = "/login");
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/challenge?X-Requested-With=XMLHttpRequest");
        Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
        var responded = transaction.Response.Headers.GetValues("Location");
        Assert.Single(responded);
        Assert.StartsWith("http://example.com/login", responded.Single());
    }

    [Fact]
    public async Task AjaxForbidTurnsInto403WithLocationHeader()
    {
        using var host = await CreateHost(o => o.AccessDeniedPath = "/denied");
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/forbid?X-Requested-With=XMLHttpRequest");
        Assert.Equal(HttpStatusCode.Forbidden, transaction.Response.StatusCode);
        var responded = transaction.Response.Headers.GetValues("Location");
        Assert.Single(responded);
        Assert.StartsWith("http://example.com/denied", responded.Single());
    }

    [Fact]
    public async Task AjaxLogoutRedirectToReturnUrlTurnsInto200WithLocationHeader()
    {
        using var host = await CreateHost(o => o.LogoutPath = "/signout");
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/signout?X-Requested-With=XMLHttpRequest&ReturnUrl=/");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        var responded = transaction.Response.Headers.GetValues("Location");
        Assert.Single(responded);
        Assert.StartsWith("/", responded.Single());
    }

    [Fact]
    public async Task AjaxChallengeRedirectTurnsInto200WithLocationHeader()
    {
        using var host = await CreateHost(s => { });
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/challenge?X-Requested-With=XMLHttpRequest&ReturnUrl=/");
        Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
        var responded = transaction.Response.Headers.GetValues("Location");
        Assert.Single(responded);
        Assert.StartsWith("http://example.com/Account/Login", responded.Single());
    }

    [Fact]
    public async Task ProtectedCustomRequestShouldRedirectToCustomRedirectUri()
    {
        using var host = await CreateHost(s => { });
        using var server = host.GetTestServer();

        var transaction = await SendAsync(server, "http://example.com/protected/CustomRedirect");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location;
        Assert.Equal("http://example.com/Account/Login?ReturnUrl=%2FCustomRedirect", location.ToString());
    }

    private static Task SignInAsAlice(HttpContext context)
    {
        var user = new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"));
        user.AddClaim(new Claim("marker", "true"));
        return context.SignInAsync("Cookies",
            new ClaimsPrincipal(user),
            new AuthenticationProperties());
    }

    private static Task SignInAsWrong(HttpContext context)
    {
        return context.SignInAsync("Oops",
            new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
            new AuthenticationProperties());
    }

    private Task SignOutAsWrong(HttpContext context)
    {
        return context.SignOutAsync("Oops");
    }

    [Fact]
    public async Task SignInCausesDefaultCookieToBeCreated()
    {
        using var host = await CreateHostWithServices(s => s.AddAuthentication().AddCookie(o =>
        {
            o.LoginPath = new PathString("/login");
            o.Cookie.Name = "TestCookie";
        }), SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath");

        var setCookie = transaction.SetCookie;
        Assert.StartsWith("TestCookie=", setCookie);
        Assert.Contains("; path=/", setCookie);
        Assert.Contains("; httponly", setCookie);
        Assert.Contains("; samesite=", setCookie);
        Assert.DoesNotContain("; expires=", setCookie);
        Assert.DoesNotContain("; domain=", setCookie);
        Assert.DoesNotContain("; secure", setCookie);
        Assert.True(transaction.Response.Headers.CacheControl.NoCache);
        Assert.True(transaction.Response.Headers.CacheControl.NoStore);
        Assert.Equal("no-cache", transaction.Response.Headers.Pragma.ToString());
    }

    private class TestTicketStore : ITicketStore
    {
        private const string KeyPrefix = "AuthSessionStore-";
        public readonly Dictionary<string, AuthenticationTicket> Store = new Dictionary<string, AuthenticationTicket>();

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            var guid = Guid.NewGuid();
            var key = KeyPrefix + guid.ToString();
            await RenewAsync(key, ticket);
            return key;
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            Store[key] = ticket;

            return Task.FromResult(0);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            AuthenticationTicket ticket;
            Store.TryGetValue(key, out ticket);
            return Task.FromResult(ticket);
        }

        public Task RemoveAsync(string key)
        {
            Store.Remove(key);
            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task SignInWithTicketStoreWorks()
    {
        var sessionStore = new TestTicketStore();
        using var host = await CreateHostWithServices(s =>
        {
            s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
            {
                o.TimeProvider = _timeProvider;
                o.SessionStore = sessionStore;
            });
        }, SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        // Make sure we have one key as the session id
        var key1 = Assert.Single(sessionStore.Store.Keys);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        // Make sure the session is expired
        _timeProvider.Advance(TimeSpan.FromDays(60));

        // Verify that a new session is generated with a new key
        var transaction3 = await SendAsync(server, "http://example.com/signinalice", transaction1.CookieNameValue);

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);

        var key2 = Assert.Single(sessionStore.Store.Keys);
        Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public async Task SessionStoreRemovesExpired()
    {
        var sessionStore = new TestTicketStore();
        using var host = await CreateHostWithServices(s =>
        {
            s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
            {
                o.TimeProvider = _timeProvider;
                o.SessionStore = sessionStore;
            });
        }, SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        // Make sure we have one key as the session id
        var key1 = Assert.Single(sessionStore.Store.Keys);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        // Make sure the session is expired
        _timeProvider.Advance(TimeSpan.FromDays(60));

        // Verify that a new session is generated with a new key
        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        Assert.Empty(sessionStore.Store.Keys);
        Assert.Null(FindClaimValue(transaction3, ClaimTypes.Name));
    }

    [Fact]
    public async Task CustomAuthSchemeEncodesCookieName()
    {
        var schemeName = "With spaces and 界";
        using var host = await CreateHostWithServices(s => s.AddAuthentication(schemeName).AddCookie(schemeName, o =>
        {
            o.LoginPath = new PathString("/login");
        }), context =>
        {
            var user = new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"));
            user.AddClaim(new Claim("marker", "true"));
            return context.SignInAsync(schemeName,
                new ClaimsPrincipal(user),
                new AuthenticationProperties());
        });

        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath");

        var setCookie = transaction.SetCookie;
        Assert.StartsWith(".AspNetCore.With%20spaces%20and%20%E7%95%8C=", setCookie);
        Assert.Contains("; path=/", setCookie);
        Assert.Contains("; httponly", setCookie);
        Assert.Contains("; samesite=", setCookie);
        Assert.DoesNotContain("; expires=", setCookie);
        Assert.DoesNotContain("; domain=", setCookie);
        Assert.DoesNotContain("; secure", setCookie);
        Assert.True(transaction.Response.Headers.CacheControl.NoCache);
        Assert.True(transaction.Response.Headers.CacheControl.NoStore);
        Assert.Equal("no-cache", transaction.Response.Headers.Pragma.ToString());
    }

    [Fact]
    public void SettingCookieExpirationOptionThrows()
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddCookie(o =>
        {
            o.Cookie.Expiration = TimeSpan.FromDays(10);
        });
        var options = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.Throws<OptionsValidationException>(() => options.Get(CookieAuthenticationDefaults.AuthenticationScheme));
    }

    [Fact]
    public async Task SignInWrongAuthTypeThrows()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = new PathString("/login");
            o.Cookie.Name = "TestCookie";
        }, SignInAsWrong);
        using var server = host.GetTestServer();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendAsync(server, "http://example.com/testpath"));
    }

    [Fact]
    public async Task SignOutWrongAuthTypeThrows()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = new PathString("/login");
            o.Cookie.Name = "TestCookie";
        }, SignOutAsWrong);

        using var server = host.GetTestServer();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendAsync(server, "http://example.com/testpath"));
    }

    [Theory]
    [InlineData(CookieSecurePolicy.Always, "http://example.com/testpath", true)]
    [InlineData(CookieSecurePolicy.Always, "https://example.com/testpath", true)]
    [InlineData(CookieSecurePolicy.None, "http://example.com/testpath", false)]
    [InlineData(CookieSecurePolicy.None, "https://example.com/testpath", false)]
    [InlineData(CookieSecurePolicy.SameAsRequest, "http://example.com/testpath", false)]
    [InlineData(CookieSecurePolicy.SameAsRequest, "https://example.com/testpath", true)]
    public async Task SecureSignInCausesSecureOnlyCookieByDefault(
        CookieSecurePolicy cookieSecurePolicy,
        string requestUri,
        bool shouldBeSecureOnly)
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = new PathString("/login");
            o.Cookie.Name = "TestCookie";
            o.Cookie.SecurePolicy = cookieSecurePolicy;
        }, SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, requestUri);
        var setCookie = transaction.SetCookie;

        if (shouldBeSecureOnly)
        {
            Assert.Contains("; secure", setCookie);
        }
        else
        {
            Assert.DoesNotContain("; secure", setCookie);
        }
    }

    [Fact]
    public async Task CookieOptionsAlterSetCookieHeader()
    {
        using var host = await CreateHost(o =>
        {
            o.Cookie.Name = "TestCookie";
            o.Cookie.Path = "/foo";
            o.Cookie.Domain = "another.com";
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.SameSite = SameSiteMode.None;
            o.Cookie.HttpOnly = true;
            o.Cookie.Extensions.Add("extension0");
            o.Cookie.Extensions.Add("extension1=value1");
        }, SignInAsAlice, baseAddress: new Uri("http://example.com/base"));

        using var server1 = host.GetTestServer();
        var transaction1 = await SendAsync(server1, "http://example.com/base/testpath");

        var setCookie1 = transaction1.SetCookie;

        Assert.Contains("TestCookie=", setCookie1);
        Assert.Contains(" path=/foo", setCookie1);
        Assert.Contains(" domain=another.com", setCookie1);
        Assert.Contains(" secure", setCookie1);
        Assert.Contains(" samesite=none", setCookie1);
        Assert.Contains(" httponly", setCookie1);
        Assert.Contains(" extension0", setCookie1);
        Assert.Contains(" extension1=value1", setCookie1);

        using var host2 = await CreateHost(o =>
        {
            o.Cookie.Name = "SecondCookie";
            o.Cookie.SecurePolicy = CookieSecurePolicy.None;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.HttpOnly = false;
        }, SignInAsAlice, baseAddress: new Uri("http://example.com/base"));

        using var server2 = host2.GetTestServer();
        var transaction2 = await SendAsync(server2, "http://example.com/base/testpath");

        var setCookie2 = transaction2.SetCookie;

        Assert.Contains("SecondCookie=", setCookie2);
        Assert.Contains(" path=/base", setCookie2);
        Assert.Contains(" samesite=strict", setCookie2);
        Assert.DoesNotContain(" domain=", setCookie2);
        Assert.DoesNotContain(" secure", setCookie2);
        Assert.DoesNotContain(" httponly", setCookie2);
        Assert.DoesNotContain(" extension", setCookie2);
    }

    [Fact]
    public async Task CookieContainsIdentity()
    {
        using var host = await CreateHost(o => { }, SignInAsAlice);
        using var server = host.GetTestServer();

        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieAppliesClaimsTransform()
    {
        using var host = await CreateHost(o => { },
        SignInAsAlice,
        baseAddress: null,
        claimsTransform: true);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        Assert.Equal("yup", FindClaimValue(transaction2, "xform"));
        Assert.Null(FindClaimValue(transaction2, "sync"));
    }

    [Fact]
    public async Task CookieStopsWorkingAfterExpiration()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
        }, SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        _timeProvider.Advance(TimeSpan.FromMinutes(7));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        _timeProvider.Advance(TimeSpan.FromMinutes(7));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        Assert.Null(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        Assert.Null(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));
        Assert.Null(transaction4.SetCookie);
        Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieExpirationCanBeOverridenInSignin()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                new AuthenticationProperties() { ExpiresUtc = _timeProvider.GetUtcNow().Add(TimeSpan.FromMinutes(5)) }));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        _timeProvider.Advance(TimeSpan.FromMinutes(3));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        _timeProvider.Advance(TimeSpan.FromMinutes(3));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

        Assert.Null(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        Assert.Null(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));
        Assert.Null(transaction4.SetCookie);
        Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));
    }

    [Fact]
    public async Task ExpiredCookieWithValidatorStillExpired()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        _timeProvider.Advance(TimeSpan.FromMinutes(11));

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction2.SetCookie);
        Assert.Null(FindClaimValue(transaction2, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieCanBeRejectedAndSignedOutByValidator()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.RejectPrincipal();
                    ctx.HttpContext.SignOutAsync("Cookies");
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Contains(".AspNetCore.Cookies=; expires=", transaction2.SetCookie);
        Assert.Null(FindClaimValue(transaction2, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieNotRenewedAfterSignOut()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        // renews on every request
        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        var transaction3 = await server.SendAsync("http://example.com/normal", transaction1.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie[0]);

        // signout wins over renew
        var transaction4 = await server.SendAsync("http://example.com/signout", transaction3.SetCookie[0]);
        Assert.Single(transaction4.SetCookie);
        Assert.Contains(".AspNetCore.Cookies=; expires=", transaction4.SetCookie[0]);
    }

    [Fact]
    public async Task CookieCanBeRenewedByValidator()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction4.SetCookie);
        Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieCanBeReplacedByValidator()
    {
        using var host = await CreateHost(o =>
        {
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    ctx.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice2", "Cookies2"))));
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("Alice2", FindClaimValue(transaction2, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieCanBeUpdatedByValidatorDuringRefresh()
    {
        var replace = false;
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    if (replace)
                    {
                        ctx.ShouldRenew = true;
                        ctx.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice2", "Cookies2"))));
                        ctx.Properties.Items["updated"] = "yes";
                    }
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        Assert.Null(FindPropertiesValue(transaction3, "updated"));

        replace = true;

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("Alice2", FindClaimValue(transaction4, ClaimTypes.Name));
        Assert.Equal("yes", FindPropertiesValue(transaction4, "updated"));

        replace = false;

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
        Assert.Equal("Alice2", FindClaimValue(transaction5, ClaimTypes.Name));
        Assert.Equal("yes", FindPropertiesValue(transaction4, "updated"));
    }

    [Fact]
    public async Task CookieCanBeRenewedByValidatorWithSlidingExpiry()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(11));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieCanBeRenewedByValidatorWithModifiedProperties()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    var id = ctx.Principal.Identities.First();
                    var claim = id.FindFirst("counter");
                    if (claim == null)
                    {
                        id.AddClaim(new Claim("counter", "1"));
                    }
                    else
                    {
                        id.RemoveClaim(claim);
                        id.AddClaim(new Claim("counter", claim.Value + "1"));
                    }
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("1", FindClaimValue(transaction2, "counter"));

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie);
        Assert.Equal("11", FindClaimValue(transaction3, "counter"));

        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("111", FindClaimValue(transaction4, "counter"));

        _timeProvider.Advance(TimeSpan.FromMinutes(11));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Null(FindClaimValue(transaction5, "counter"));
    }

    [Fact]
    public async Task CookieCanBeRenewedByValidatorWithModifiedLifetime()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    var id = ctx.Principal.Identities.First();
                    var claim = id.FindFirst("counter");
                    if (claim == null)
                    {
                        id.AddClaim(new Claim("counter", "1"));
                    }
                    else
                    {
                        id.RemoveClaim(claim);
                        id.AddClaim(new Claim("counter", claim.Value + "1"));
                    }
                    // Causes the expiry time to not be extended because the lifetime is
                    // calculated relative to the issue time.
                    ctx.Properties.IssuedUtc = _timeProvider.GetUtcNow();
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("1", FindClaimValue(transaction2, "counter"));

        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie);
        Assert.Equal("11", FindClaimValue(transaction3, "counter"));

        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("111", FindClaimValue(transaction4, "counter"));

        _timeProvider.Advance(TimeSpan.FromMinutes(9));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Null(FindClaimValue(transaction5, "counter"));
    }

    [Fact]
    public async Task CookieValidatorOnlyCalledOnce()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    ctx.ShouldRenew = true;
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(6));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction4.SetCookie);
        Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldRenewUpdatesIssuedExpiredUtc(bool sliding)
    {
        DateTimeOffset? lastValidateIssuedDate = null;
        DateTimeOffset? lastExpiresDate = null;
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = sliding;
            o.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = ctx =>
                {
                    lastValidateIssuedDate = ctx.Properties.IssuedUtc;
                    lastExpiresDate = ctx.Properties.ExpiresUtc;
                    ctx.ShouldRenew = true;
                    return Task.FromResult(0);
                }
            };
        },
        context =>
            context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        Assert.NotNull(lastValidateIssuedDate);
        Assert.NotNull(lastExpiresDate);

        var firstIssueDate = lastValidateIssuedDate;
        var firstExpiresDate = lastExpiresDate;

        _timeProvider.Advance(TimeSpan.FromMinutes(1));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
        Assert.NotNull(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(2));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

        Assert.NotEqual(lastValidateIssuedDate, firstIssueDate);
        Assert.NotEqual(firstExpiresDate, lastExpiresDate);
    }

    [Fact]
    public async Task CookieExpirationCanBeOverridenInEvent()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = false;
            o.Events = new CookieAuthenticationEvents()
            {
                OnSigningIn = context =>
                {
                    context.Properties.ExpiresUtc = _timeProvider.GetUtcNow().Add(TimeSpan.FromMinutes(5));
                    return Task.FromResult(0);
                }
            };
        },
        SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(3));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(3));

        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction4.SetCookie);
        Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieIsRenewedWithSlidingExpiration()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = true;
        },
        SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        // transaction4 should arrive with a new SetCookie value
        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction5, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieIsRenewedWithSlidingExpirationWithoutTransformations()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = true;
            o.Events.OnValidatePrincipal = c =>
            {
                // https://github.com/aspnet/Security/issues/1607
                // On sliding refresh the transformed principal should not be serialized into the cookie, only the original principal.
                Assert.Single(c.Principal.Identities);
                Assert.True(c.Principal.Identities.First().HasClaim("marker", "true"));
                return Task.CompletedTask;
            };
        },
        SignInAsAlice,
        claimsTransform: true);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.Null(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        // transaction4 should arrive with a new SetCookie value
        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
        Assert.NotNull(transaction4.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
        Assert.Null(transaction5.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction5, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieIsRenewedWithSlidingExpirationEvent()
    {
        using var host = await CreateHost(o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            o.SlidingExpiration = true;
            o.Events = new CookieAuthenticationEvents()
            {
                OnCheckSlidingExpiration = context =>
                {
                    var expectRenew = string.Equals("1", context.Request.Query["expectrenew"]);
                    var renew = string.Equals("1", context.Request.Query["renew"]);
                    Assert.Equal(expectRenew, context.ShouldRenew);
                    context.ShouldRenew = renew;
                    return Task.CompletedTask;
                }
            };
        },
        SignInAsAlice);

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/me/Cookies?expectrenew=0&renew=0", transaction1.CookieNameValue);
        Assert.Null(transaction2.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        var transaction3 = await SendAsync(server, "http://example.com/me/Cookies?expectrenew=0&renew=0", transaction1.CookieNameValue);
        Assert.Null(transaction3.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

        _timeProvider.Advance(TimeSpan.FromMinutes(4));

        // A renewal is now expected, but we've suppressed it
        var transaction4 = await SendAsync(server, "http://example.com/me/Cookies?expectrenew=1&renew=0", transaction1.CookieNameValue);
        Assert.Null(transaction4.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

        // Allow the default renewal to happen
        var transaction5 = await SendAsync(server, "http://example.com/me/Cookies?expectrenew=1&renew=1", transaction1.CookieNameValue);
        Assert.NotNull(transaction5.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction5, ClaimTypes.Name));

        // Force a renewal on an un-expired new cookie
        var transaction6 = await SendAsync(server, "http://example.com/me/Cookies?expectrenew=0&renew=1", transaction5.CookieNameValue);
        Assert.NotNull(transaction5.SetCookie);
        Assert.Equal("Alice", FindClaimValue(transaction6, ClaimTypes.Name));
    }

    [Fact]
    public async Task CookieUsesPathBaseByDefault()
    {
        using var host = await CreateHost(o => { },
        context =>
        {
            Assert.Equal(new PathString("/base"), context.Request.PathBase);
            return context.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))));
        },
        new Uri("http://example.com/base"));

        using var server = host.GetTestServer();
        var transaction1 = await SendAsync(server, "http://example.com/base/testpath");
        Assert.Contains("path=/base", transaction1.SetCookie);
    }

    [Fact]
    public async Task CookieChallengeRedirectsToLoginWithoutCookie()
    {
        using var host = await CreateHost(o => { }, SignInAsAlice);

        var url = "http://example.com/challenge";
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, url);

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location;
        Assert.Equal("/Account/Login", location.LocalPath);
    }

    [Fact]
    public async Task CookieForbidRedirectsWithoutCookie()
    {
        using var host = await CreateHost(o => { }, SignInAsAlice);

        var url = "http://example.com/forbid";
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, url);

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        var location = transaction.Response.Headers.Location;
        Assert.Equal("/Account/AccessDenied", location.LocalPath);
    }

    [Fact]
    public async Task CookieChallengeRedirectsWithLoginPath()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = new PathString("/page");
        });
        using var server = host.GetTestServer();

        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/challenge", transaction1.CookieNameValue);

        Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
    }

    [Fact]
    public async Task CookieChallengeWithUnauthorizedRedirectsToLoginIfNotAuthenticated()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = new PathString("/page");
        });
        using var server = host.GetTestServer();

        var transaction1 = await SendAsync(server, "http://example.com/testpath");

        var transaction2 = await SendAsync(server, "http://example.com/unauthorized", transaction1.CookieNameValue);

        Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MapWillAffectChallengeOnlyWithUseAuth(bool useAuth)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        if (useAuth)
                        {
                            app.UseAuthentication();
                        }
                        app.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Cookies", new AuthenticationProperties() { RedirectUri = "/" })));
                    })
                    .ConfigureServices(s => s.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/page"))))
            .Build();
        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com/login");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        var location = transaction.Response.Headers.Location;
        if (useAuth)
        {
            Assert.Equal("/page", location.LocalPath);
        }
        else
        {
            Assert.Equal("/login/page", location.LocalPath);
        }
        Assert.Equal("?ReturnUrl=%2F", location.Query);
    }

    [ConditionalFact(Skip = "Revisit, exception no longer thrown")]
    public async Task ChallengeDoesNotSet401OnUnauthorized()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async context =>
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(() => context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme));
                    });
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie()))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task CanConfigureDefaultCookieInstance()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Run(context => context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity("whatever"))));
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddAuthentication().AddCookie();
                        services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme,
                            o => o.Cookie.Name = "One");
                    }))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com");

        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.StartsWith("One=", transaction.SetCookie[0]);
    }

    [Fact]
    public async Task CanConfigureNamedCookieInstance()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Run(context => context.SignInAsync("Cookie1", new ClaimsPrincipal(new ClaimsIdentity("whatever"))));
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddAuthentication().AddCookie("Cookie1");
                        services.Configure<CookieAuthenticationOptions>("Cookie1",
                            o => o.Cookie.Name = "One");
                    }))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com");

        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.StartsWith("One=", transaction.SetCookie[0]);
    }

    [Fact]
    public async Task MapWithSignInOnlyRedirectToReturnUrlOnLoginPath()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Map("/notlogin", signoutApp => signoutApp.Run(context => context.SignInAsync("Cookies",
                            new ClaimsPrincipal(new ClaimsIdentity("whatever")))));
                    })
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/login"))))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com/notlogin?ReturnUrl=%2Fpage");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.NotNull(transaction.SetCookie);
    }

    [Fact]
    public async Task MapWillNotAffectSignInRedirectToReturnUrl()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Map("/login", signoutApp => signoutApp.Run(context => context.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity("whatever")))));
                    })
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/login"))))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com/login?ReturnUrl=%2Fpage");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.NotNull(transaction.SetCookie);

        var location = transaction.Response.Headers.Location;
        Assert.Equal("/page", location.OriginalString);
    }

    [Fact]
    public async Task MapWithSignOutOnlyRedirectToReturnUrlOnLogoutPath()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Map("/notlogout", signoutApp => signoutApp.Run(context => context.SignOutAsync("Cookies")));
                    })
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LogoutPath = new PathString("/logout"))))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com/notlogout?ReturnUrl=%2Fpage");
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Contains(".AspNetCore.Cookies=; expires=", transaction.SetCookie[0]);
    }

    [Fact]
    public async Task MapWillNotAffectSignOutRedirectToReturnUrl()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Map("/logout", signoutApp => signoutApp.Run(context => context.SignOutAsync("Cookies")));
                    })
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LogoutPath = new PathString("/logout"))))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var transaction = await server.SendAsync("http://example.com/logout?ReturnUrl=%2Fpage");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Contains(".AspNetCore.Cookies=; expires=", transaction.SetCookie[0]);

        var location = transaction.Response.Headers.Location;
        Assert.Equal("/page", location.OriginalString);
    }

    [Fact]
    public async Task MapWillNotAffectAccessDenied()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Map("/forbid", signoutApp => signoutApp.Run(context => context.ForbidAsync("Cookies")));
                    })
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.AccessDeniedPath = new PathString("/denied"))))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/forbid");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        var location = transaction.Response.Headers.Location;
        Assert.Equal("/denied", location.LocalPath);
    }

    [Fact]
    public async Task NestedMapWillNotAffectLogin()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                        app.Map("/base", map =>
                        {
                            map.UseAuthentication();
                            map.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Cookies", new AuthenticationProperties() { RedirectUri = "/" })));
                        }))
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/page"))))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/base/login");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        var location = transaction.Response.Headers.Location;
        Assert.Equal("/base/page", location.LocalPath);
        Assert.Equal("?ReturnUrl=%2F", location.Query);
    }

    [Theory]
    [InlineData("/redirect_test", "/loginpath")]
    [InlineData("/redirect_test", "/testpath")]
    [InlineData("http://example.com/redirect_to", "/loginpath")]
    [InlineData("http://example.com/redirect_to", "/testpath")]
    public async Task RedirectUriIsHonoredAfterSignin(string redirectUrl, string loginPath)
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = loginPath;
            o.Cookie.Name = "TestCookie";
        },
        async context =>
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))),
                new AuthenticationProperties { RedirectUri = redirectUrl })
        );
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath");

        Assert.NotEmpty(transaction.SetCookie);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal(redirectUrl, transaction.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task RedirectUriInQueryIsIgnoredAfterSigninForUnrecognizedEndpoints()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = "/loginpath";
            o.ReturnUrlParameter = "return";
            o.Cookie.Name = "TestCookie";
        },
        async context =>
        {
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))));
        });
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath?return=%2Fret_path_2");

        Assert.NotEmpty(transaction.SetCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task RedirectUriInQueryIsHonoredAfterSignin()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = "/testpath";
            o.ReturnUrlParameter = "return";
            o.Cookie.Name = "TestCookie";
        },
        async context =>
        {
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))));
        });
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath?return=%2Fret_path_2");

        Assert.NotEmpty(transaction.SetCookie);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/ret_path_2", transaction.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task AbsoluteRedirectUriInQueryStringIsRejected()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = "/testpath";
            o.ReturnUrlParameter = "return";
            o.Cookie.Name = "TestCookie";
        },
        async context =>
        {
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))));
        });
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath?return=http%3A%2F%2Fexample.com%2Fredirect_to");

        Assert.NotEmpty(transaction.SetCookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task EnsurePrecedenceOfRedirectUriAfterSignin()
    {
        using var host = await CreateHost(o =>
        {
            o.LoginPath = "/testpath";
            o.ReturnUrlParameter = "return";
            o.Cookie.Name = "TestCookie";
        },
        async context =>
        {
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))),
                new AuthenticationProperties { RedirectUri = "/redirect_test" });
        });
        using var server = host.GetTestServer();
        var transaction = await SendAsync(server, "http://example.com/testpath?return=%2Fret_path_2");

        Assert.NotEmpty(transaction.SetCookie);
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.Equal("/redirect_test", transaction.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task NestedMapWillNotAffectAccessDenied()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                        app.Map("/base", map =>
                        {
                            map.UseAuthentication();
                            map.Map("/forbid", signoutApp => signoutApp.Run(context => context.ForbidAsync("Cookies")));
                        }))
                        .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.AccessDeniedPath = new PathString("/denied"))))
            .Build();
        await host.StartAsync();
        using var server = host.GetTestServer();
        var transaction = await server.SendAsync("http://example.com/base/forbid");

        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

        var location = transaction.Response.Headers.Location;
        Assert.Equal("/base/denied", location.LocalPath);
    }

    [Fact]
    public async Task CanSpecifyAndShareDataProtector()
    {
        var dp = new NoOpDataProtector();
        using var host1 = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Run((context) =>
                            context.SignInAsync("Cookies",
                                            new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                                            new AuthenticationProperties()));
                    })
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o =>
                    {
                        o.TicketDataFormat = new TicketDataFormat(dp);
                        o.Cookie.Name = "Cookie";
                    })))
            .Build();
        await host1.StartAsync();
        using var server1 = host1.GetTestServer();

        var transaction = await SendAsync(server1, "http://example.com/stuff");
        Assert.NotNull(transaction.SetCookie);

        using var host2 = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async (context) =>
                    {
                        var result = await context.AuthenticateAsync("Cookies");
                        await DescribeAsync(context.Response, result);
                    });
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie("Cookies", o =>
                {
                    o.Cookie.Name = "Cookie";
                    o.TicketDataFormat = new TicketDataFormat(dp);
                })))
            .Build();
        await host2.StartAsync();
        using var server2 = host2.GetTestServer();
        var transaction2 = await SendAsync(server2, "http://example.com/stuff", transaction.CookieNameValue);
        Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
    }

    // Issue: https://github.com/aspnet/Security/issues/949
    [Fact]
    public async Task NullExpiresUtcPropertyIsGuarded()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o =>
                {
                    o.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = context =>
                        {
                            context.Properties.ExpiresUtc = null;
                            context.ShouldRenew = true;
                            return Task.FromResult(0);
                        }
                    };
                }))
                .Configure(app =>
                {
                    app.UseAuthentication();

                    app.Run(async context =>
                    {
                        if (context.Request.Path == "/signin")
                        {
                            await context.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))));
                        }
                        else
                        {
                            await context.Response.WriteAsync("ha+1");
                        }
                    });
                }))
            .Build();

        await host.StartAsync();
        using var server = host.GetTestServer();

        var cookie = (await server.SendAsync("http://www.example.com/signin")).SetCookie.FirstOrDefault();
        Assert.NotNull(cookie);

        var transaction = await server.SendAsync("http://www.example.com/", cookie);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    private class NoOpDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public byte[] Protect(byte[] plaintext)
        {
            return plaintext;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return protectedData;
        }
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

    private static string FindPropertiesValue(Transaction transaction, string key)
    {
        var property = transaction.ResponseElement.Elements("extra").SingleOrDefault(elt => elt.Attribute("type").Value == key);
        if (property == null)
        {
            return null;
        }
        return property.Attribute("value").Value;
    }

    private class ClaimsTransformer : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal p)
        {
            var firstId = p.Identities.First();
            if (firstId.HasClaim("marker", "true"))
            {
                firstId.RemoveClaim(firstId.FindFirst("marker"));
            }
            // TransformAsync could be called twice on one request if you have a default scheme and also
            // call AuthenticateAsync.
            if (!p.Identities.Any(i => i.AuthenticationType == "xform"))
            {
                var id = new ClaimsIdentity("xform");
                id.AddClaim(new Claim("xform", "yup"));
                p.AddIdentity(id);
            }
            return Task.FromResult(p);
        }
    }

    private Task<IHost> CreateHost(Action<CookieAuthenticationOptions> configureOptions, Func<HttpContext, Task> testpath = null, Uri baseAddress = null, bool claimsTransform = false)
        => CreateHostWithServices(s =>
        {
            s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
            {
                o.TimeProvider = _timeProvider;
                configureOptions(o);
            });
            if (claimsTransform)
            {
                s.AddSingleton<IClaimsTransformation, ClaimsTransformer>();
            }
        }, testpath, baseAddress);

    private static async Task<IHost> CreateHostWithServices(Action<IServiceCollection> configureServices, Func<HttpContext, Task> testpath = null, Uri baseAddress = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
                    .Configure(app =>
                    {
                        app.UseAuthentication();
                        app.Use(async (context, next) =>
                        {
                            var req = context.Request;
                            var res = context.Response;
                            PathString remainder;
                            if (req.Path == new PathString("/normal"))
                            {
                                res.StatusCode = 200;
                            }
                            else if (req.Path == new PathString("/forbid")) // Simulate forbidden
                            {
                                await context.ForbidAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                            else if (req.Path == new PathString("/challenge"))
                            {
                                await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                            else if (req.Path == new PathString("/signinalice"))
                            {
                                await SignInAsAlice(context);
                            }
                            else if (req.Path == new PathString("/signout"))
                            {
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                            else if (req.Path == new PathString("/unauthorized"))
                            {
                                await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties());
                            }
                            else if (req.Path == new PathString("/protected/CustomRedirect"))
                            {
                                await context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties() { RedirectUri = "/CustomRedirect" });
                            }
                            else if (req.Path == new PathString("/me"))
                            {
                                await DescribeAsync(res, AuthenticateResult.Success(new AuthenticationTicket(context.User, new AuthenticationProperties(), CookieAuthenticationDefaults.AuthenticationScheme)));
                            }
                            else if (req.Path.StartsWithSegments(new PathString("/me"), out remainder))
                            {
                                var ticket = await context.AuthenticateAsync(remainder.Value.Substring(1));
                                await DescribeAsync(res, ticket);
                            }
                            else if (req.Path == new PathString("/testpath") && testpath != null)
                            {
                                await testpath(context);
                            }
                            else if (req.Path == new PathString("/checkforerrors"))
                            {
                                var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme); // this used to be "Automatic"
                                if (result.Failure != null)
                                {
                                    throw new Exception("Failed to authenticate", result.Failure);
                                }
                                return;
                            }
                            else
                            {
                                await next(context);
                            }
                        });
                    })
                    .ConfigureServices(configureServices))
            .Build();

        await host.StartAsync();

        var server = host.GetTestServer();
        server.BaseAddress = baseAddress;
        return host;
    }

    private static Task DescribeAsync(HttpResponse res, AuthenticateResult result)
    {
        res.StatusCode = 200;
        res.ContentType = "text/xml";
        var xml = new XElement("xml");
        if (result?.Ticket?.Principal != null)
        {
            xml.Add(result.Ticket.Principal.Claims.Select(claim => new XElement("claim", new XAttribute("type", claim.Type), new XAttribute("value", claim.Value))));
        }
        if (result?.Ticket?.Properties != null)
        {
            xml.Add(result.Ticket.Properties.Items.Select(extra => new XElement("extra", new XAttribute("type", extra.Key), new XAttribute("value", extra.Value))));
        }
        var xmlBytes = Encoding.UTF8.GetBytes(xml.ToString());
        return res.Body.WriteAsync(xmlBytes, 0, xmlBytes.Length);
    }

    private static async Task<Transaction> SendAsync(TestServer server, string uri, string cookieHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }
        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };
        if (transaction.Response.Headers.Contains("Set-Cookie"))
        {
            transaction.SetCookie = transaction.Response.Headers.GetValues("Set-Cookie").SingleOrDefault();
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
