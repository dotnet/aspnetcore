// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.CookiePolicy.Test
{
    public class CookiePolicyTests
    {
        private RequestDelegate SecureCookieAppends = context =>
        {
            context.Response.Cookies.Append("A", "A");
            context.Response.Cookies.Append("B", "B", new CookieOptions { Secure = false });
            context.Response.Cookies.Append("C", "C", new CookieOptions());
            context.Response.Cookies.Append("D", "D", new CookieOptions { Secure = true });
            return Task.FromResult(0);
        };
        private RequestDelegate HttpCookieAppends = context =>
        {
            context.Response.Cookies.Append("A", "A");
            context.Response.Cookies.Append("B", "B", new CookieOptions { HttpOnly = false });
            context.Response.Cookies.Append("C", "C", new CookieOptions());
            context.Response.Cookies.Append("D", "D", new CookieOptions { HttpOnly = true });
            return Task.FromResult(0);
        };
        private RequestDelegate SameSiteCookieAppends = context =>
        {
            context.Response.Cookies.Append("A", "A");
            context.Response.Cookies.Append("B", "B", new CookieOptions());
            context.Response.Cookies.Append("C", "C", new CookieOptions { SameSite = Http.SameSiteMode.None });
            context.Response.Cookies.Append("D", "D", new CookieOptions { SameSite = Http.SameSiteMode.Lax });
            context.Response.Cookies.Append("E", "E", new CookieOptions { SameSite = Http.SameSiteMode.Strict });
            context.Response.Cookies.Append("F", "F", new CookieOptions { SameSite = (Http.SameSiteMode)(-1) });
            return Task.FromResult(0);
        };

        [Fact]
        public async Task SecureAlwaysSetsSecure()
        {
            await RunTest("/secureAlways",
                new CookiePolicyOptions
                {
                    Secure = CookieSecurePolicy.Always
                },
                SecureCookieAppends,
                new RequestTest("http://example.com/secureAlways",
                    transaction =>
                    {
                        Assert.NotNull(transaction.SetCookie);
                        Assert.Equal("A=A; path=/; secure", transaction.SetCookie[0]);
                        Assert.Equal("B=B; path=/; secure", transaction.SetCookie[1]);
                        Assert.Equal("C=C; path=/; secure", transaction.SetCookie[2]);
                        Assert.Equal("D=D; path=/; secure", transaction.SetCookie[3]);
                    }));
        }

        [Fact]
        public async Task SecureNoneLeavesSecureUnchanged()
        {
            await RunTest("/secureNone",
                new CookiePolicyOptions
                {
                    Secure = CookieSecurePolicy.None
                },
                SecureCookieAppends,
                new RequestTest("http://example.com/secureNone",
                    transaction =>
                    {
                        Assert.NotNull(transaction.SetCookie);
                        Assert.Equal("A=A; path=/", transaction.SetCookie[0]);
                        Assert.Equal("B=B; path=/", transaction.SetCookie[1]);
                        Assert.Equal("C=C; path=/", transaction.SetCookie[2]);
                        Assert.Equal("D=D; path=/; secure", transaction.SetCookie[3]);
                    }));
        }

        [Fact]
        public async Task SecureSameUsesRequest()
        {
            await RunTest("/secureSame",
                new CookiePolicyOptions
                {
                    Secure = CookieSecurePolicy.SameAsRequest
                },
                SecureCookieAppends,
                new RequestTest("http://example.com/secureSame",
                    transaction =>
                    {
                        Assert.NotNull(transaction.SetCookie);
                        Assert.Equal("A=A; path=/", transaction.SetCookie[0]);
                        Assert.Equal("B=B; path=/", transaction.SetCookie[1]);
                        Assert.Equal("C=C; path=/", transaction.SetCookie[2]);
                        Assert.Equal("D=D; path=/; secure", transaction.SetCookie[3]);
                    }),
                new RequestTest("https://example.com/secureSame",
                    transaction =>
                    {
                        Assert.NotNull(transaction.SetCookie);
                        Assert.Equal("A=A; path=/; secure", transaction.SetCookie[0]);
                        Assert.Equal("B=B; path=/; secure", transaction.SetCookie[1]);
                        Assert.Equal("C=C; path=/; secure", transaction.SetCookie[2]);
                        Assert.Equal("D=D; path=/; secure", transaction.SetCookie[3]);
                    }));
        }

        [Fact]
        public async Task HttpOnlyAlwaysSetsItAlways()
        {
            await RunTest("/httpOnlyAlways",
                new CookiePolicyOptions
                {
                    HttpOnly = HttpOnlyPolicy.Always
                },
                HttpCookieAppends,
                new RequestTest("http://example.com/httpOnlyAlways",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/; httponly", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/; httponly", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/; httponly", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/; httponly", transaction.SetCookie[3]);
                }));
        }

        [Fact]
        public async Task HttpOnlyNoneLeavesItAlone()
        {
            await RunTest("/httpOnlyNone",
                new CookiePolicyOptions
                {
                    HttpOnly = HttpOnlyPolicy.None
                },
                HttpCookieAppends,
                new RequestTest("http://example.com/httpOnlyNone",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/; httponly", transaction.SetCookie[3]);
                }));
        }

        [Fact]
        public async Task SameSiteStrictSetsItAlways()
        {
            await RunTest("/sameSiteStrict",
                new CookiePolicyOptions
                {
                    MinimumSameSitePolicy = Http.SameSiteMode.Strict
                },
                SameSiteCookieAppends,
                new RequestTest("http://example.com/sameSiteStrict",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/; samesite=strict", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/; samesite=strict", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/; samesite=strict", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/; samesite=strict", transaction.SetCookie[3]);
                    Assert.Equal("E=E; path=/; samesite=strict", transaction.SetCookie[4]);
                }));
        }

        [Fact]
        public async Task SameSiteLaxSetsItAlways()
        {
            await RunTest("/sameSiteLax",
                new CookiePolicyOptions
                {
                    MinimumSameSitePolicy = Http.SameSiteMode.Lax
                },
                SameSiteCookieAppends,
                new RequestTest("http://example.com/sameSiteLax",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/; samesite=lax", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/; samesite=lax", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/; samesite=lax", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/; samesite=lax", transaction.SetCookie[3]);
                    Assert.Equal("E=E; path=/; samesite=strict", transaction.SetCookie[4]);
                }));
        }

        [Fact]
        public async Task SameSiteNoneSetsItAlways()
        {
            await RunTest("/sameSiteNone",
                new CookiePolicyOptions
                {
                    MinimumSameSitePolicy = Http.SameSiteMode.None
                },
                SameSiteCookieAppends,
                new RequestTest("http://example.com/sameSiteNone",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/; samesite=none", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/; samesite=none", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/; samesite=none", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/; samesite=lax", transaction.SetCookie[3]);
                    Assert.Equal("E=E; path=/; samesite=strict", transaction.SetCookie[4]);
                }));
        }

        [Fact]
        public async Task SameSiteUnspecifiedLeavesItAlone()
        {
            await RunTest("/sameSiteNone",
                new CookiePolicyOptions
                {
                    MinimumSameSitePolicy = Http.SameSiteMode.Unspecified
                },
                SameSiteCookieAppends,
                new RequestTest("http://example.com/sameSiteNone",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/; samesite=none", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/; samesite=lax", transaction.SetCookie[3]);
                    Assert.Equal("E=E; path=/; samesite=strict", transaction.SetCookie[4]);
                    Assert.Equal("F=F; path=/", transaction.SetCookie[5]);
                }));
        }

        [Fact]
        public async Task CookiePolicyCanHijackAppend()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCookiePolicy(new CookiePolicyOptions
                    {
                        OnAppendCookie = ctx => ctx.CookieName = ctx.CookieValue = "Hao"
                    });
                    app.Run(context =>
                    {
                        context.Response.Cookies.Append("A", "A");
                        context.Response.Cookies.Append("B", "B", new CookieOptions { Secure = false });
                        context.Response.Cookies.Append("C", "C", new CookieOptions() { SameSite = Http.SameSiteMode.Strict });
                        context.Response.Cookies.Append("D", "D", new CookieOptions { Secure = true });
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.NotNull(transaction.SetCookie);
            Assert.Equal("Hao=Hao; path=/", transaction.SetCookie[0]);
            Assert.Equal("Hao=Hao; path=/", transaction.SetCookie[1]);
            Assert.Equal("Hao=Hao; path=/; samesite=strict", transaction.SetCookie[2]);
            Assert.Equal("Hao=Hao; path=/; secure", transaction.SetCookie[3]);
        }

        [Fact]
        public async Task CookiePolicyCanHijackDelete()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
            {
                app.UseCookiePolicy(new CookiePolicyOptions
                {
                    OnDeleteCookie = ctx => ctx.CookieName = "A"
                });
                app.Run(context =>
                {
                    context.Response.Cookies.Delete("A");
                    context.Response.Cookies.Delete("B", new CookieOptions { Secure = false });
                    context.Response.Cookies.Delete("C", new CookieOptions());
                    context.Response.Cookies.Delete("D", new CookieOptions { Secure = true });
                    return Task.FromResult(0);
                });
            });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.NotNull(transaction.SetCookie);
            Assert.Equal(1, transaction.SetCookie.Count);
            Assert.Equal("A=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; secure", transaction.SetCookie[0]);
        }

        [Fact]
        public async Task CookiePolicyCallsCookieFeature()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
            {
                app.Use(next => context =>
                {
                    context.Features.Set<IResponseCookiesFeature>(new TestCookieFeature());
                    return next(context);
                });
                app.UseCookiePolicy(new CookiePolicyOptions
                {
                    OnDeleteCookie = ctx => ctx.CookieName = "A"
                });
                app.Run(context =>
                {
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Delete("A"));
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Delete("A", new CookieOptions()));
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Append("A", "A"));
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Append("A", "A", new CookieOptions()));
                    return context.Response.WriteAsync("Done");
                });
            });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login");
            Assert.Equal("Done", transaction.ResponseText);
        }

        [Fact]
        public async Task CookiePolicyAppliesToCookieAuth()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication().AddCookie(o =>
                    {
                        o.Cookie.Name = "TestCookie";
                        o.Cookie.HttpOnly = false;
                        o.Cookie.SecurePolicy = CookieSecurePolicy.None;
                    });
                })
                .Configure(app =>
                {
                    app.UseCookiePolicy(new CookiePolicyOptions
                    {
                        HttpOnly = HttpOnlyPolicy.Always,
                        Secure = CookieSecurePolicy.Always,
                    });
                    app.UseAuthentication();
                    app.Run(context =>
                    {
                        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity("TestUser", "Cookies"))));
                    });
                });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.NotNull(transaction.SetCookie);
            Assert.Equal(1, transaction.SetCookie.Count);
            var cookie = SetCookieHeaderValue.Parse(transaction.SetCookie[0]);
            Assert.Equal("TestCookie", cookie.Name);
            Assert.True(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal("/", cookie.Path);
        }

        [Fact]
        public async Task CookiePolicyAppliesToCookieAuthChunks()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication().AddCookie(o =>
                    {
                        o.Cookie.Name = "TestCookie";
                        o.Cookie.HttpOnly = false;
                        o.Cookie.SecurePolicy = CookieSecurePolicy.None;
                    });
                })
                .Configure(app =>
                {
                    app.UseCookiePolicy(new CookiePolicyOptions
                    {
                        HttpOnly = HttpOnlyPolicy.Always,
                        Secure = CookieSecurePolicy.Always,
                    });
                    app.UseAuthentication();
                    app.Run(context =>
                    {
                        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(new ClaimsIdentity(new GenericIdentity(new string('c', 1024 * 5), "Cookies"))));
                    });
                });
            var server = new TestServer(builder);

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.NotNull(transaction.SetCookie);
            Assert.Equal(3, transaction.SetCookie.Count);

            var cookie = SetCookieHeaderValue.Parse(transaction.SetCookie[0]);
            Assert.Equal("TestCookie", cookie.Name);
            Assert.Equal("chunks-2", cookie.Value);
            Assert.True(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal("/", cookie.Path);

            cookie = SetCookieHeaderValue.Parse(transaction.SetCookie[1]);
            Assert.Equal("TestCookieC1", cookie.Name);
            Assert.True(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal("/", cookie.Path);

            cookie = SetCookieHeaderValue.Parse(transaction.SetCookie[2]);
            Assert.Equal("TestCookieC2", cookie.Name);
            Assert.True(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal("/", cookie.Path);
        }

        private class TestCookieFeature : IResponseCookiesFeature
        {
            public IResponseCookies Cookies { get; } = new BadCookies();

            private class BadCookies : IResponseCookies
            {
                public void Append(string key, string value)
                {
                    throw new NotImplementedException();
                }

                public void Append(string key, string value, CookieOptions options)
                {
                    throw new NotImplementedException();
                }

                public void Delete(string key)
                {
                    throw new NotImplementedException();
                }

                public void Delete(string key, CookieOptions options)
                {
                    throw new NotImplementedException();
                }
            }
        }

        private class RequestTest
        {
            public RequestTest(string testUri, Action<Transaction> verify)
            {
                TestUri = testUri;
                Verification = verify;
            }

            public async Task Execute(TestServer server)
            {
                var transaction = await server.SendAsync(TestUri);
                Verification(transaction);
            }

            public string TestUri { get; set; }
            public Action<Transaction> Verification { get; set; }
        }

        private async Task RunTest(
            string path,
            CookiePolicyOptions cookiePolicy,
            RequestDelegate configureSetup,
            params RequestTest[] tests)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Map(path, map =>
                    {
                        map.UseCookiePolicy(cookiePolicy);
                        map.Run(configureSetup);
                    });
                });
            var server = new TestServer(builder);
            foreach (var test in tests)
            {
                await test.Execute(server);
            }
        }
    }
}
