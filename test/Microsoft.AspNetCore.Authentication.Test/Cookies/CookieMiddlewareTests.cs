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
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    public class CookieMiddlewareTests
    {
        [Fact]
        public async Task NormalRequestPassesThrough()
        {
            var server = CreateServer(new CookieAuthenticationOptions());
            var response = await server.CreateClient().GetAsync("http://example.com/normal");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AjaxLoginRedirectToReturnUrlTurnsInto200WithLocationHeader()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                AutomaticChallenge = true,
                LoginPath = "/login"
            });

            var transaction = await SendAsync(server, "http://example.com/protected?X-Requested-With=XMLHttpRequest");
            Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Equal(1, responded.Count());
            Assert.True(responded.Single().StartsWith("http://example.com/login"));
        }

        [Fact]
        public async Task AjaxForbidTurnsInto403WithLocationHeader()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                AccessDeniedPath = "/denied"
            });

            var transaction = await SendAsync(server, "http://example.com/forbid?X-Requested-With=XMLHttpRequest");
            Assert.Equal(HttpStatusCode.Forbidden, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Equal(1, responded.Count());
            Assert.True(responded.Single().StartsWith("http://example.com/denied"));
        }

        [Fact]
        public async Task AjaxLogoutRedirectToReturnUrlTurnsInto200WithLocationHeader()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                LogoutPath = "/signout"
            });

            var transaction = await SendAsync(server, "http://example.com/signout?X-Requested-With=XMLHttpRequest&ReturnUrl=/");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Equal(1, responded.Count());
            Assert.True(responded.Single().StartsWith("/"));
        }

        [Fact]
        public async Task AjaxChallengeRedirectTurnsInto200WithLocationHeader()
        {
            var server = CreateServer(new CookieAuthenticationOptions());

            var transaction = await SendAsync(server, "http://example.com/challenge?X-Requested-With=XMLHttpRequest&ReturnUrl=/");
            Assert.Equal(HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
            var responded = transaction.Response.Headers.GetValues("Location");
            Assert.Equal(1, responded.Count());
            Assert.True(responded.Single().StartsWith("http://example.com/Account/Login"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProtectedRequestShouldRedirectToLoginOnlyWhenAutomatic(bool auto)
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/login"),
                AutomaticChallenge = auto
            });

            var transaction = await SendAsync(server, "http://example.com/protected");

            Assert.Equal(auto ? HttpStatusCode.Redirect : HttpStatusCode.Unauthorized, transaction.Response.StatusCode);
            if (auto)
            {
                var location = transaction.Response.Headers.Location;
                Assert.Equal("/login", location.LocalPath);
                Assert.Equal("?ReturnUrl=%2Fprotected", location.Query);
            }
        }

        [Fact]
        public async Task ProtectedCustomRequestShouldRedirectToCustomRedirectUri()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                AutomaticChallenge = true
            });

            var transaction = await SendAsync(server, "http://example.com/protected/CustomRedirect");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location;
            Assert.Equal("http://example.com/Account/Login?ReturnUrl=%2FCustomRedirect", location.ToString());
        }

        private Task SignInAsAlice(HttpContext context)
        {
            return context.Authentication.SignInAsync("Cookies",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                new AuthenticationProperties());
        }

        private Task SignInAsWrong(HttpContext context)
        {
            return context.Authentication.SignInAsync("Oops",
                new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                new AuthenticationProperties());
        }

        private Task SignOutAsWrong(HttpContext context)
        {
            return context.Authentication.SignOutAsync("Oops");
        }

        [Fact]
        public async Task SignInCausesDefaultCookieToBeCreated()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/login"),
                CookieName = "TestCookie"
            }, SignInAsAlice);

            var transaction = await SendAsync(server, "http://example.com/testpath");

            var setCookie = transaction.SetCookie;
            Assert.StartsWith("TestCookie=", setCookie);
            Assert.Contains("; path=/", setCookie);
            Assert.Contains("; httponly", setCookie);
            Assert.DoesNotContain("; expires=", setCookie);
            Assert.DoesNotContain("; domain=", setCookie);
            Assert.DoesNotContain("; secure", setCookie);
        }

        [Fact]
        public async Task SignInWrongAuthTypeThrows()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/login"),
                CookieName = "TestCookie"
            }, SignInAsWrong);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SendAsync(server, "http://example.com/testpath"));
        }

        [Fact]
        public async Task SignOutWrongAuthTypeThrows()
        {
            var server = CreateServer(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/login"),
                CookieName = "TestCookie"
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
            var server = CreateServer(new CookieAuthenticationOptions
            {
                LoginPath = new PathString("/login"),
                CookieName = "TestCookie",
                CookieSecure = cookieSecurePolicy
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
            TestServer server1 = CreateServer(new CookieAuthenticationOptions
            {
                CookieName = "TestCookie",
                CookiePath = "/foo",
                CookieDomain = "another.com",
                CookieSecure = CookieSecurePolicy.Always,
                CookieHttpOnly = true
            }, SignInAsAlice, new Uri("http://example.com/base"));

            var transaction1 = await SendAsync(server1, "http://example.com/base/testpath");

            var setCookie1 = transaction1.SetCookie;

            Assert.Contains("TestCookie=", setCookie1);
            Assert.Contains(" path=/foo", setCookie1);
            Assert.Contains(" domain=another.com", setCookie1);
            Assert.Contains(" secure", setCookie1);
            Assert.Contains(" httponly", setCookie1);

            var server2 = CreateServer(new CookieAuthenticationOptions
            {
                CookieName = "SecondCookie",
                CookieSecure = CookieSecurePolicy.None,
                CookieHttpOnly = false
            }, SignInAsAlice, new Uri("http://example.com/base"));

            var transaction2 = await SendAsync(server2, "http://example.com/base/testpath");

            var setCookie2 = transaction2.SetCookie;

            Assert.Contains("SecondCookie=", setCookie2);
            Assert.Contains(" path=/base", setCookie2);
            Assert.DoesNotContain(" domain=", setCookie2);
            Assert.DoesNotContain(" secure", setCookie2);
            Assert.DoesNotContain(" httponly", setCookie2);
        }

        [Fact]
        public async Task CookieContainsIdentity()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock
            }, SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieAppliesClaimsTransform()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock
            },
            SignInAsAlice,
            baseAddress: null,
            claimsTransform: new ClaimsTransformationOptions
            {
                Transformer = new ClaimsTransformer
                {
                    OnTransform = context =>
                    {
                        if (!context.Principal.Identities.Any(i => i.AuthenticationType == "xform"))
                        {
                            // REVIEW: Xform runs twice, once on Authenticate, and then once from the middleware
                            var id = new ClaimsIdentity("xform");
                            id.AddClaim(new Claim("xform", "yup"));
                            context.Principal.AddIdentity(id);
                        }
                        return Task.FromResult(context.Principal);
                    }
                }
            });

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
            Assert.Equal("yup", FindClaimValue(transaction2, "xform"));
            Assert.Null(FindClaimValue(transaction2, "sync"));
        }

        [Fact]
        public async Task CookieStopsWorkingAfterExpiration()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = false
            }, SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(7));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(7));

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
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = false
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                    new AuthenticationProperties() { ExpiresUtc = clock.UtcNow.Add(TimeSpan.FromMinutes(5)) }));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(3));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);

            clock.Add(TimeSpan.FromMinutes(3));

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
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ctx =>
                    {
                        ctx.ShouldRenew = true;
                        return Task.FromResult(0);
                    }
                }
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            clock.Add(TimeSpan.FromMinutes(11));

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Null(FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeRejectedAndSignedOutByValidator()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = false,
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ctx =>
                    {
                        ctx.RejectPrincipal();
                        ctx.HttpContext.Authentication.SignOutAsync("Cookies");
                        return Task.FromResult(0);
                    }
                }
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Contains(".AspNetCore.Cookies=; expires=", transaction2.SetCookie);
            Assert.Null(FindClaimValue(transaction2, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeRenewedByValidator()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = false,
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ctx =>
                    {
                        ctx.ShouldRenew = true;
                        return Task.FromResult(0);
                    }
                }
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction4.SetCookie);
            Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(5));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieCanBeRenewedByValidatorWithSlidingExpiry()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ctx =>
                    {
                        ctx.ShouldRenew = true;
                        return Task.FromResult(0);
                    }
                }
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(11));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieValidatorOnlyCalledOnce()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = false,
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ctx =>
                    {
                        ctx.ShouldRenew = true;
                        return Task.FromResult(0);
                    }
                }
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(5));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(6));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction4.SetCookie);
            Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(5));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Null(FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldRenewUpdatesIssuedExpiredUtc(bool sliding)
        {
            var clock = new TestClock();
            DateTimeOffset? lastValidateIssuedDate = null;
            DateTimeOffset? lastExpiresDate = null;
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = sliding,
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ctx =>
                    {
                        lastValidateIssuedDate = ctx.Properties.IssuedUtc;
                        lastExpiresDate = ctx.Properties.ExpiresUtc;
                        ctx.ShouldRenew = true;
                        return Task.FromResult(0);
                    }
                }
            },
            context =>
                context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies")))));

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            Assert.NotNull(lastValidateIssuedDate);
            Assert.NotNull(lastExpiresDate);

            var firstIssueDate = lastValidateIssuedDate;
            var firstExpiresDate = lastExpiresDate;

            clock.Add(TimeSpan.FromMinutes(1));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction2.CookieNameValue);
            Assert.NotNull(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(2));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction3.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            Assert.NotEqual(lastValidateIssuedDate, firstIssueDate);
            Assert.NotEqual(firstExpiresDate, lastExpiresDate);
        }

        [Fact]
        public async Task CookieExpirationCanBeOverridenInEvent()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = false,
                Events = new CookieAuthenticationEvents()
                {
                    OnSigningIn = context =>
                    {
                        context.Properties.ExpiresUtc = clock.UtcNow.Add(TimeSpan.FromMinutes(5));
                        return Task.FromResult(0);
                    }
                }
            }, SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(3));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(3));

            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction4.SetCookie);
            Assert.Null(FindClaimValue(transaction4, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieIsRenewedWithSlidingExpiration()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                ExpireTimeSpan = TimeSpan.FromMinutes(10),
                SlidingExpiration = true
            }, SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction2.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(4));

            var transaction3 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.Null(transaction3.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction3, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(4));

            // transaction4 should arrive with a new SetCookie value
            var transaction4 = await SendAsync(server, "http://example.com/me/Cookies", transaction1.CookieNameValue);
            Assert.NotNull(transaction4.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction4, ClaimTypes.Name));

            clock.Add(TimeSpan.FromMinutes(4));

            var transaction5 = await SendAsync(server, "http://example.com/me/Cookies", transaction4.CookieNameValue);
            Assert.Null(transaction5.SetCookie);
            Assert.Equal("Alice", FindClaimValue(transaction5, ClaimTypes.Name));
        }

        [Fact]
        public async Task CookieUsesPathBaseByDefault()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions(),
            context =>
            {
                Assert.Equal(new PathString("/base"), context.Request.PathBase);
                return context.Authentication.SignInAsync("Cookies",
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))));
            },
            new Uri("http://example.com/base"));

            var transaction1 = await SendAsync(server, "http://example.com/base/testpath");
            Assert.True(transaction1.SetCookie.Contains("path=/base"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CookieTurnsChallengeIntoForbidWithCookie(bool automatic)
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = automatic,
                SystemClock = clock
            },
            SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var url = "http://example.com/challenge";
            var transaction2 = await SendAsync(server, url, transaction1.CookieNameValue);

            Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
            var location = transaction2.Response.Headers.Location;
            Assert.Equal("/Account/AccessDenied", location.LocalPath);
            Assert.Equal("?ReturnUrl=%2Fchallenge", location.Query);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CookieChallengeRedirectsToLoginWithoutCookie(bool automatic)
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = automatic,
                SystemClock = clock
            },
            SignInAsAlice);

            var url = "http://example.com/challenge";
            var transaction = await SendAsync(server, url);

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location;
            Assert.Equal("/Account/Login", location.LocalPath);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CookieForbidRedirectsWithoutCookie(bool automatic)
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = automatic,
                SystemClock = clock
            },
            SignInAsAlice);

            var url = "http://example.com/forbid";
            var transaction = await SendAsync(server, url);

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            var location = transaction.Response.Headers.Location;
            Assert.Equal("/Account/AccessDenied", location.LocalPath);
        }

        [Fact]
        public async Task CookieTurns401ToAccessDeniedWhenSetWithCookie()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                AccessDeniedPath = new PathString("/accessdenied")
            },
            SignInAsAlice);

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/challenge", transaction1.CookieNameValue);

            Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);

            var location = transaction2.Response.Headers.Location;
            Assert.Equal("/accessdenied", location.LocalPath);
        }

        [Fact]
        public async Task CookieChallengeRedirectsWithLoginPath()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                LoginPath = new PathString("/page")
            });

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/challenge", transaction1.CookieNameValue);

            Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
        }

        [Fact]
        public async Task CookieChallengeWithUnauthorizedRedirectsToLoginIfNotAuthenticated()
        {
            var clock = new TestClock();
            var server = CreateServer(new CookieAuthenticationOptions
            {
                SystemClock = clock,
                LoginPath = new PathString("/page")
            });

            var transaction1 = await SendAsync(server, "http://example.com/testpath");

            var transaction2 = await SendAsync(server, "http://example.com/unauthorized", transaction1.CookieNameValue);

            Assert.Equal(HttpStatusCode.Redirect, transaction2.Response.StatusCode);
        }

        [Fact]
        public async Task MapWillNotAffectChallenge()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        LoginPath = new PathString("/page")
                    });
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Cookies", new AuthenticationProperties() { RedirectUri = "/" })));
                })
                .ConfigureServices(services => services.AddAuthentication());
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/page", location.LocalPath);
            Assert.Equal("?ReturnUrl=%2F", location.Query);
        }

        [Fact]
        public async Task ChallengeDoesNotSet401OnUnauthorized()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication();
                    app.Run(async context =>
                    {
                        await Assert.ThrowsAsync<InvalidOperationException>(() => context.Authentication.ChallengeAsync());
                    });
                })
                .ConfigureServices(services => services.AddAuthentication());
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task UseCookieWithInstanceDoesntUseSharedOptions()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        CookieName = "One"
                    });
                    app.UseCookieAuthentication();
                    app.Run(context => context.Authentication.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity())));
                })
                .ConfigureServices(services => services.AddAuthentication());
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com");

            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
            Assert.True(transaction.SetCookie[0].StartsWith(".AspNetCore.Cookies="));
        }

        [Fact]
        public async Task MapWithSignInOnlyRedirectToReturnUrlOnLoginPath()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        LoginPath = new PathString("/login")
                    });
                    app.Map("/notlogin", signoutApp => signoutApp.Run(context => context.Authentication.SignInAsync("Cookies",
                        new ClaimsPrincipal())));
                })
                .ConfigureServices(services => services.AddAuthentication());
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
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        LoginPath = new PathString("/login")
                    });
                    app.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.SignInAsync("Cookies",
                        new ClaimsPrincipal())));
                })
                .ConfigureServices(services => services.AddAuthentication());
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
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        LogoutPath = new PathString("/logout")
                    });
                    app.Map("/notlogout", signoutApp => signoutApp.Run(context => context.Authentication.SignOutAsync("Cookies")));
                })
                .ConfigureServices(services => services.AddAuthentication());
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
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        LogoutPath = new PathString("/logout")
                    });
                    app.Map("/logout", signoutApp => signoutApp.Run(context => context.Authentication.SignOutAsync("Cookies")));
                })
                .ConfigureServices(services => services.AddAuthentication());
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
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AccessDeniedPath = new PathString("/denied")
                    });
                    app.Map("/forbid", signoutApp => signoutApp.Run(context => context.Authentication.ForbidAsync("Cookies")));
                })
                .ConfigureServices(services => services.AddAuthentication());
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
                        map.UseCookieAuthentication(new CookieAuthenticationOptions
                        {
                            LoginPath = new PathString("/page")
                        });
                        map.Map("/login", signoutApp => signoutApp.Run(context => context.Authentication.ChallengeAsync("Cookies", new AuthenticationProperties() { RedirectUri = "/" })));
                    }))
                .ConfigureServices(services => services.AddAuthentication());
            var server = new TestServer(builder);
            var transaction = await server.SendAsync("http://example.com/base/login");

            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);

            var location = transaction.Response.Headers.Location;
            Assert.Equal("/base/page", location.LocalPath);
            Assert.Equal("?ReturnUrl=%2F", location.Query);
        }

        [Fact]
        public async Task RedirectUriIsHoneredAfterSignin()
        {
            var options = new CookieAuthenticationOptions
            {
                LoginPath = "/testpath",
                CookieName = "TestCookie"
            };

            var server = CreateServer(options, async context =>
            {
                await context.Authentication.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))),
                    new AuthenticationProperties { RedirectUri = "/redirect_test" });
            });
            var transaction = await SendAsync(server, "http://example.com/testpath");

            Assert.NotEmpty(transaction.SetCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/redirect_test", transaction.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task EnsurePrecedenceOfRedirectUriAfterSignin()
        {
            var options = new CookieAuthenticationOptions
            {
                LoginPath = "/testpath",
                ReturnUrlParameter = "return",
                CookieName = "TestCookie"
            };

            var server = CreateServer(options, async context =>
            {
                await context.Authentication.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", CookieAuthenticationDefaults.AuthenticationScheme))),
                    new AuthenticationProperties { RedirectUri = "/redirect_test" });
            });
            var transaction = await SendAsync(server, "http://example.com/testpath?return=%2Fret_path_2");

            Assert.NotEmpty(transaction.SetCookie);
            Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
            Assert.Equal("/ret_path_2", transaction.Response.Headers.Location.ToString());
        }

        [Fact]
        public async Task NestedMapWillNotAffectAccessDenied()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                    app.Map("/base", map =>
                    {
                        map.UseCookieAuthentication(new CookieAuthenticationOptions
                        {
                            AccessDeniedPath = new PathString("/denied")
                        });
                        map.Map("/forbid", signoutApp => signoutApp.Run(context => context.Authentication.ForbidAsync("Cookies")));
                    }))
                    .ConfigureServices(services => services.AddAuthentication());
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
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        TicketDataFormat = new TicketDataFormat(dp),
                        CookieName = "Cookie"
                    });
                    app.Use((context, next) =>
                        context.Authentication.SignInAsync("Cookies",
                                        new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("Alice", "Cookies"))),
                                        new AuthenticationProperties()));
                })
                .ConfigureServices(services => services.AddAuthentication());
            var server1 = new TestServer(builder1);

            var transaction = await SendAsync(server1, "http://example.com/stuff");
            Assert.NotNull(transaction.SetCookie);

            var builder2 = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(new CookieAuthenticationOptions
                    {
                        AuthenticationScheme = "Cookies",
                        CookieName = "Cookie",
                        TicketDataFormat = new TicketDataFormat(dp)
                    });
                    app.Use(async (context, next) =>
                    {
                        var authContext = new AuthenticateContext("Cookies");
                        await context.Authentication.AuthenticateAsync(authContext);
                        Describe(context.Response, authContext);
                    });
                })
                .ConfigureServices(services => services.AddAuthentication());
            var server2 = new TestServer(builder2);
            var transaction2 = await SendAsync(server2, "http://example.com/stuff", transaction.CookieNameValue);
            Assert.Equal("Alice", FindClaimValue(transaction2, ClaimTypes.Name));
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

        private static async Task<XElement> GetAuthData(TestServer server, string url, string cookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", cookie);

            var response2 = await server.CreateClient().SendAsync(request);
            var text = await response2.Content.ReadAsStringAsync();
            var me = XElement.Parse(text);
            return me;
        }

        private static TestServer CreateServer(CookieAuthenticationOptions options, Func<HttpContext, Task> testpath = null, Uri baseAddress = null, ClaimsTransformationOptions claimsTransform = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookieAuthentication(options);
                    // app.UseCookieAuthentication(new CookieAuthenticationOptions { AuthenticationScheme = "Cookie2" });

                    if (claimsTransform != null)
                    {
                        app.UseClaimsTransformation(claimsTransform);
                    }
                    app.Use(async (context, next) =>
                    {
                        var req = context.Request;
                        var res = context.Response;
                        PathString remainder;
                        if (req.Path == new PathString("/normal"))
                        {
                            res.StatusCode = 200;
                        }
                        else if (req.Path == new PathString("/protected"))
                        {
                            res.StatusCode = 401;
                        }
                        else if (req.Path == new PathString("/forbid")) // Simulate forbidden 
                        {
                            await context.Authentication.ForbidAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString("/challenge"))
                        {
                            await context.Authentication.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString("/signout"))
                        {
                            await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        }
                        else if (req.Path == new PathString("/unauthorized"))
                        {
                            await context.Authentication.ChallengeAsync(CookieAuthenticationDefaults.AuthenticationScheme, new AuthenticationProperties(), ChallengeBehavior.Unauthorized);
                        }
                        else if (req.Path == new PathString("/protected/CustomRedirect"))
                        {
                            await context.Authentication.ChallengeAsync(new AuthenticationProperties() { RedirectUri = "/CustomRedirect" });
                        }
                        else if (req.Path == new PathString("/me"))
                        {
                            var authContext = new AuthenticateContext(CookieAuthenticationDefaults.AuthenticationScheme);
                            authContext.Authenticated(context.User, properties: null, description: null);
                            Describe(res, authContext);
                        }
                        else if (req.Path.StartsWithSegments(new PathString("/me"), out remainder))
                        {
                            var authContext = new AuthenticateContext(remainder.Value.Substring(1));
                            await context.Authentication.AuthenticateAsync(authContext);
                            Describe(res, authContext);
                        }
                        else if (req.Path == new PathString("/testpath") && testpath != null)
                        {
                            await testpath(context);
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services => services.AddAuthentication());
            var server = new TestServer(builder);
            server.BaseAddress = baseAddress;
            return server;
        }

        private static void Describe(HttpResponse res, AuthenticateContext result)
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
                xml.Add(result.Properties.Select(extra => new XElement("extra", new XAttribute("type", extra.Key), new XAttribute("value", extra.Value))));
            }
            var xmlBytes = Encoding.UTF8.GetBytes(xml.ToString());
            res.Body.Write(xmlBytes, 0, xmlBytes.Length);
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
