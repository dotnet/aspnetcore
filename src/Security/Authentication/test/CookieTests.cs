// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    public class CookieTests : SharedAuthenticationTests<CookieAuthenticationOptions>
    {
        private TestClock _clock = new TestClock();

        protected override string DefaultScheme => CookieAuthenticationDefaults.AuthenticationScheme;
        protected override Type HandlerType => typeof(CookieAuthenticationHandler);

        protected override void RegisterAuth(AuthenticationBuilder services, Action<CookieAuthenticationOptions> configure)
        {
            services.AddCookie(configure);
        }

        [Fact]
        public async Task NormalRequestPassesThrough()
        {
            var server = CreateServer(s => { });
            var response = await server.CreateClient().GetAsync("http://example.com/normal");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AjaxLoginRedirectToReturnUrlTurnsInto200WithLocationHeader()
        {
            var server = CreateServer(o => o.LoginPath = "/login");
            var transaction = await SendAsync(server, "http://example.com/challenge?X-Requested-With=XMLHttpRequest");
            Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Single(responded);
            Assert.StartsWith("http://example.com/login", responded.Single());
        }

        [Fact]
        public async Task AjaxForbidTurnsInto403WithLocationHeader()
        {
            var server = CreateServer(o => o.AccessDeniedPath = "/denied");
            var transaction = await SendAsync(server, "http://example.com/forbid?X-Requested-With=XMLHttpRequest");
            Assert.Equal(HttpStatusCode.Forbidden, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Single(responded);
            Assert.StartsWith("http://example.com/denied", responded.Single());
        }

        [Fact]
        public async Task AjaxLogoutRedirectToReturnUrlTurnsInto200WithLocationHeader()
        {
            var server = CreateServer(o => o.LogoutPath = "/signout");
            var transaction = await SendAsync(server, "http://example.com/signout?X-Requested-With=XMLHttpRequest&ReturnUrl=/");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Single(responded);
            Assert.StartsWith("/", responded.Single());
        }

        [Fact]
        public async Task AjaxChallengeRedirectTurnsInto200WithLocationHeader()
        {
            var server = CreateServer(s => { });
            var transaction = await SendAsync(server, "http://example.com/challenge?X-Requested-With=XMLHttpRequest&ReturnUrl=/");
            Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Single(responded);
            Assert.StartsWith("http://example.com/Account/Login", responded.Single());
        }

        [Fact]
        public async Task ProtectedCustomRequestShouldRedirectToCustomRedirectUri()
        {
            var server = CreateServer(s => { });

            var transaction = await SendAsync(server, "http://example.com/protected/CustomRedirect");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location;
            Assert.Equal("http://example.com/Account/Login?ReturnUrl=%2FCustomRedirect", location.ToString());
        }

        private Task SignInAsAlice(HttpContext context)
        {
            var user = new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"));
            user.AddClaim(new Claim("marker", "true"));
            return context.SignInAsync("Cookies",
                new ClaimsPrincipal(user),
                new AuthenticationProperties());
        }

        private Task SignInAsWrong(HttpContext context)
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
            var server = CreateServerWithServices(s => s.AddAuthentication().AddCookie(o =>
            {
                o.LoginPath = new PathString("/login");
                o.Cookie.Name = "TestCookie";
            }), SignInAsAlice);

            var transaction = await SendAsync(server, "http://example.com/testpath");

            var setCookie = transaction.SetCookie;
            Assert.StartsWith("TestCookie=", setCookie);
            Assert.Contains("; path=/", setCookie);
            Assert.Contains("; httponly", setCookie);
            Assert.Contains("; samesite=", setCookie);
            Assert.DoesNotContain("; expires=", setCookie);
            Assert.DoesNotContain("; domain=", setCookie);
            Assert.DoesNotContain("; secure", setCookie);
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
            var server = CreateServer(o =>
            {
                o.LoginPath = new PathString("/login");
                o.Cookie.Name = "TestCookie";
            }, SignInAsWrong);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendAsync(server, "http://example.com/testpath"));
        }

        [Fact]
        public async Task SignOutWrongAuthTypeThrows()
        {
            var server = CreateServer(o =>
            {
                o.LoginPath = new PathString("/login");
                o.Cookie.Name = "TestCookie";
            }, SignOutAsWrong);

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
            var server = CreateServer(o =>
            {
                o.LoginPath = new PathString("/login");
                o.Cookie.Name = "TestCookie";
                o.Cookie.SecurePolicy = cookieSecurePolicy;
            }, SignInAsAlice);

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
            var server1 = CreateServer(o =>
            {
                o.Cookie.Name = "TestCookie";
                o.Cookie.Path = "/foo";
                o.Cookie.Domain = "another.com";
                o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                o.Cookie.SameSite = SameSiteMode.None;
                o.Cookie.HttpOnly = true;
            }, SignInAsAlice, baseAddress: new Uri("http://example.com/base"));

            var transaction1 = await SendAsync(server1, "http://example.com/base/testpath");

            var setCookie1 = transaction1.SetCookie;

            Assert.Contains("TestCookie=", setCookie1);
            Assert.Contains(" path=/foo", setCookie1);
            Assert.Contains(" domain=another.com", setCookie1);
            Assert.Contains(" secure", setCookie1);
            Assert.Contains(" samesite=none", setCookie1);
            Assert.Contains(" httponly", setCookie1);

            var server2 = CreateServer(o =>
            {
                o.Cookie.Name = "SecondCookie";
                o.Cookie.SecurePolicy = CookieSecurePolicy.None;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.Cookie.HttpOnly = false;
            }, SignInAsAlice, baseAddress: new Uri("http://example.com/base"));

            var transaction2 = await SendAsync(server2, "http://example.com/base/testpath");

            var setCookie2 = transaction2.SetCookie;

            Assert.Contains("SecondCookie=", setCookie2);
            Assert.Contains(" path=/base", setCookie2);
            Assert.Contains(" samesite=strict", setCookie2);
            Assert.DoesNotContain(" domain=", setCookie2);
            Assert.DoesNotContain(" secure", setCookie2);
            Assert.DoesNotContain(" httponly", setCookie2);
        }

        [Fact]
        public async Task CookieContainsIdentity()
        {
            var server = CreateServer(o => { }, SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieAppliesClaimsTransform()
        {
            var server = CreateServer(o => { },
            SignInAsAlice,
            baseAddress: null,
            claimsTransform: true);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
            Assert.Equal("yup", FindClaimValue(transaction2, "xform"));
            Assert.Null(FindClaimValue(transaction2, "sync"));
        }

        [Fact]
        public async Task CookieStopsWorkingAfterExpiration()
        {
            var server = CreateServer(o =>
            {
                o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                o.SlidingExpiration = false;
            }, SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            _clock.Add(TimeSpan.FromMinutes(7));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            _clock.Add(TimeSpan.FromMinutes(7));

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
            var server = CreateServer(o =>
            {
                o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                o.SlidingExpiration = false;
            },
            context =>
                context.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                    new AuthenticationProperties() { ExpiresUtc = _clock.UtcNow.Add(TimeSpan.FromMinutes(5)) }));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            _clock.Add(TimeSpan.FromMinutes(3));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            _clock.Add(TimeSpan.FromMinutes(3));

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
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            _clock.Add(TimeSpan.FromMinutes(11));

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Null(FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeRejectedAndSignedOutByValidator()
        {
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Contains(".AspNetCore.Cookies=; expires=", transaction2.SetCookie);
            Assert.Null(FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieNotRenewedAfterSignOut()
        {
            var server = CreateServer(o =>
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
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction4.SetCookie);
            Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(5));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeReplacedByValidator()
        {
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice2", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeUpdatedByValidatorDuringRefresh()
        {
            var replace = false;
            var server = CreateServer(o =>
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
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(11));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeRenewedByValidatorWithModifiedProperties()
        {
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("1", FindClaimValue(transaction2, "counter"));

            _clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("11", FindClaimValue(transaction3, "counter"));

            _clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("111", FindClaimValue(transaction4, "counter"));

            _clock.Add(TimeSpan.FromMinutes(11));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Null(FindClaimValue(transaction5, "counter"));
        }

        [Fact]
        public async Task CookieValidatorOnlyCalledOnce()
        {
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction4.SetCookie);
            Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(5));

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
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            Assert.NotNull(lastValidateIssuedDate);
            Assert.NotNull(lastExpiresDate);

            var firstIssueDate = lastValidateIssuedDate;
            var firstExpiresDate = lastExpiresDate;

            _clock.Add(TimeSpan.FromMinutes(1));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(2));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            Assert.NotEqual(lastValidateIssuedDate, firstIssueDate);
            Assert.NotEqual(firstExpiresDate, lastExpiresDate);
        }

        [Fact]
        public async Task CookieExpirationCanBeOverridenInEvent()
        {
            var server = CreateServer(o =>
            {
                o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                o.SlidingExpiration = false;
                o.Events = new CookieAuthenticationEvents()
                {
                    OnSigningIn = context =>
                    {
                        context.Properties.ExpiresUtc = _clock.UtcNow.Add(TimeSpan.FromMinutes(5));
                        return Task.FromResult(0);
                    }
                };
            },
            SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(3));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(3));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction4.SetCookie);
            Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieIsRenewedWithSlidingExpiration()
        {
            var server = CreateServer(o =>
            {
                o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                o.SlidingExpiration = true;
            },
            SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(4));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(4));

            // transaction4 should arrive with a new SetCookie value
            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(4));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieIsRenewedWithSlidingExpirationWithoutTransformations()
        {
            var server = CreateServer(o =>
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

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(4));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(4));

            // transaction4 should arrive with a new SetCookie value
            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            _clock.Add(TimeSpan.FromMinutes(4));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieUsesPathBaseByDefault()
        {
            var server = CreateServer(o => { },
            context =>
            {
                Assert.Equal(new PathString("/base"), context.Request.PathBase);
                return context.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))));
            },
            new Uri("http://example.com/base"));

            var transaction1 = await SendAsync(server, "http://example.com/base/testpath");
            Assert.Contains("path=/base", transaction1.SetCookie);
        }

        [Fact]
        public async Task CookieChallengeRedirectsToLoginWithoutCookie()
        {
            var server = CreateServer(o => { }, SignInAsAlice);

            var url = "http://example.com/challenge";
            var transaction = await SendAsync(server, url);

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location;
            Assert.Equal("/Account/Login", location.LocalPath);
        }

        [Fact]
        public async Task CookieForbidRedirectsWithoutCookie()
        {
            var server = CreateServer(o => { }, SignInAsAlice);

            var url = "http://example.com/forbid";
            var transaction = await SendAsync(server, url);

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location;
            Assert.Equal("/Account/AccessDenied", location.LocalPath);
        }

        [Fact]
        public async Task CookieChallengeRedirectsWithLoginPath()
        {
            var server = CreateServer(o =>
            {
                o.LoginPath = new PathString("/page");
            });

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/challenge", transaction1.CookieNameValue);

            Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
        }

        [Fact]
        public async Task CookieChallengeWithUnauthorizedRedirectsToLoginIfNotAuthenticated()
        {
            var server = CreateServer(o =>
            {
                o.LoginPath = new PathString("/page");
            });

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/unauthorized", transaction1.CookieNameValue);

            Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task MapWillAffectChallengeOnlyWithUseAuth(bool useAuth)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    if (useAuth)
                    {
                        app.UseAuthentication();
                    }
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Cookies", new AuthenticationProperties() { RedirectUri = "/" })));
                })
                .ConfigureServices(s => s.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/page")));
            var server = new TestServer(builder);

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
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(async context =>
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(() => context.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme));
                    });
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie());
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task CanConfigureDefaultCookieInstance()
        {
            var builder = new WebHostBuilder()
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
                });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com");

            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.StartsWith("One=", transaction.SetCookie[0]);
        }

        [Fact]
        public async Task CanConfigureNamedCookieInstance()
        {
            var builder = new WebHostBuilder()
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
                });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com");

            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.StartsWith("One=", transaction.SetCookie[0]);
        }

        [Fact]
        public async Task MapWithSignInOnlyRedirectToReturnUrlOnLoginPath()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/notlogin", signoutApp => signoutApp.Run(context => context.SignInAsync("Cookies",
                        new ClaimsPrincipal(new ClaimsIdentity("whatever")))));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/login")));
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/notlogin?ReturnUrl=%2Fpage");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.NotNull(transaction.SetCookie);
        }

        [Fact]
        public async Task MapWillNotAffectSignInRedirectToReturnUrl()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity("whatever")))));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/login")));

            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login?ReturnUrl=%2Fpage");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.NotNull(transaction.SetCookie);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/page", location.OriginalString);
        }

        [Fact]
        public async Task MapWithSignOutOnlyRedirectToReturnUrlOnLogoutPath()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/notlogout", signoutApp => signoutApp.Run(context => context.SignOutAsync("Cookies")));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LogoutPath = new PathString("/logout")));
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/notlogout?ReturnUrl=%2Fpage");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.Contains(".AspNetCore.Cookies=; expires=", transaction.SetCookie[0]);
        }

        [Fact]
        public async Task MapWillNotAffectSignOutRedirectToReturnUrl()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/logout", signoutApp => signoutApp.Run(context => context.SignOutAsync("Cookies")));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LogoutPath = new PathString("/logout")));
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/logout?ReturnUrl=%2Fpage");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Contains(".AspNetCore.Cookies=; expires=", transaction.SetCookie[0]);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/page", location.OriginalString);
        }

        [Fact]
        public async Task MapWillNotAffectAccessDenied()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Map("/forbid", signoutApp => signoutApp.Run(context => context.ForbidAsync("Cookies")));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.AccessDeniedPath = new PathString("/denied")));
            var server = new TestServer(builder);
            var transaction = await server.SendAsync("http://example.com/forbid");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/denied", location.LocalPath);
        }

        [Fact]
        public async Task NestedMapWillNotAffectLogin()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                    app.Map("/base", map =>
                    {
                        map.UseAuthentication();
                        map.Map("/login", signoutApp => signoutApp.Run(context => context.ChallengeAsync("Cookies", new AuthenticationProperties() { RedirectUri = "/" })));
                    }))
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.LoginPath = new PathString("/page")));
            var server = new TestServer(builder);
            var transaction = await server.SendAsync("http://example.com/base/login");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/base/page", location.LocalPath);
            Assert.Equal("?ReturnUrl=%2F", location.Query);
        }

        [Theory]
        [InlineData("/redirect_test")]
        [InlineData("http://example.com/redirect_to")]
        public async Task RedirectUriIsHoneredAfterSignin(string redirectUrl)
        {
            var server = CreateServer(o =>
            {
                o.LoginPath = "/testpath";
                o.Cookie.Name = "TestCookie";
            },
            async context =>
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))),
                    new AuthenticationProperties { RedirectUri = redirectUrl })
            );
            var transaction = await SendAsync(server, "http://example.com/testpath");

            Assert.NotEmpty(transaction.SetCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal(redirectUrl, transaction.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task RedirectUriInQueryIsHoneredAfterSignin()
        {
            var server = CreateServer(o =>
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
            var transaction = await SendAsync(server, "http://example.com/testpath?return=%2Fret_path_2");

            Assert.NotEmpty(transaction.SetCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/ret_path_2", transaction.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task AbsoluteRedirectUriInQueryStringIsRejected()
        {
            var server = CreateServer(o =>
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
            var transaction = await SendAsync(server, "http://example.com/testpath?return=http%3A%2F%2Fexample.com%2Fredirect_to");

            Assert.NotEmpty(transaction.SetCookie);
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task EnsurePrecedenceOfRedirectUriAfterSignin()
        {
            var server = CreateServer(o =>
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
            var transaction = await SendAsync(server, "http://example.com/testpath?return=%2Fret_path_2");

            Assert.NotEmpty(transaction.SetCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/redirect_test", transaction.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task NestedMapWillNotAffectAccessDenied()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                    app.Map("/base", map =>
                    {
                        map.UseAuthentication();
                        map.Map("/forbid", signoutApp => signoutApp.Run(context => context.ForbidAsync("Cookies")));
                    }))
                    .ConfigureServices(services => services.AddAuthentication().AddCookie(o => o.AccessDeniedPath = new PathString("/denied")));
            var server = new TestServer(builder);
            var transaction = await server.SendAsync("http://example.com/base/forbid");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/base/denied", location.LocalPath);
        }

        [Fact]
        public async Task CanSpecifyAndShareDataProtector()
        {

            var dp = new NoOpDataProtector();
            var builder1 = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use((context, next) =>
                        context.SignInAsync("Cookies",
                                        new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                                        new AuthenticationProperties()));
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie(o =>
                {
                    o.TicketDataFormat = new TicketDataFormat(dp);
                    o.Cookie.Name = "Cookie";
                }));
            var server1 = new TestServer(builder1);

            var transaction = await SendAsync(server1, "http://example.com/stuff");
            Assert.NotNull(transaction.SetCookie);

            var builder2 = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Use(async (context, next) =>
                    {
                        var result = await context.AuthenticateAsync("Cookies");
                        await DescribeAsync(context.Response, result);
                    });
                })
                .ConfigureServices(services => services.AddAuthentication().AddCookie("Cookies", o =>
                {
                    o.Cookie.Name = "Cookie";
                    o.TicketDataFormat = new TicketDataFormat(dp);
                }));
            var server2 = new TestServer(builder2);
            var transaction2 = await SendAsync(server2, "http://example.com/stuff", transaction.CookieNameValue);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        // Issue: https://github.com/aspnet/Security/issues/949
        [Fact]
        public async Task NullExpiresUtcPropertyIsGuarded()
        {
            var builder = new WebHostBuilder()
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
                });

            var server = new TestServer(builder);

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

        private static async Task<XElement> GetAuthData(TestServer server, string url, string cookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", cookie);

            var response2 = await server.CreateClient().SendAsync(request);
            var text = await response2.Content.ReadAsStringAsync();
            var me = XElement.Parse(text);
            return me;
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

        private TestServer CreateServer(Action<CookieAuthenticationOptions> configureOptions, Func<HttpContext, Task> testpath = null, Uri baseAddress = null, bool claimsTransform = false)
            => CreateServerWithServices(s =>
            {
                s.AddSingleton<ISystemClock>(_clock);
                s.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(configureOptions);
                if (claimsTransform)
                {
                    s.AddSingleton<IClaimsTransformation, ClaimsTransformer>();
                }
            }, testpath, baseAddress);

        private static TestServer CreateServerWithServices(Action<IServiceCollection> configureServices, Func<HttpContext, Task> testpath = null, Uri baseAddress = null)
        {
            var builder = new WebHostBuilder()
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
                            await next();
                        }
                    });
                })
                .ConfigureServices(configureServices);
            var server = new TestServer(builder);
            server.BaseAddress = baseAddress;
            return server;
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
}
