// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.CookiePolicy.Test
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

        [Fact]
        public async Task SecureAlwaysSetsSecure()
        {
            await RunTest("/secureAlways",
                options => options.Secure = SecurePolicy.Always,
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
                options => options.Secure = SecurePolicy.None,
                SecureCookieAppends,
                new RequestTest("http://example.com/secureNone",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
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
                options => options.Secure = SecurePolicy.SameAsRequest,
                SecureCookieAppends,
                new RequestTest("http://example.com/secureSame",
                transaction =>
                {
                    Assert.NotNull(transaction.SetCookie);
                    Assert.Equal("A=A; path=/", transaction.SetCookie[0]);
                    Assert.Equal("B=B; path=/", transaction.SetCookie[1]);
                    Assert.Equal("C=C; path=/", transaction.SetCookie[2]);
                    Assert.Equal("D=D; path=/", transaction.SetCookie[3]);
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
                options => options.HttpOnly = HttpOnlyPolicy.Always,
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
                options => options.HttpOnly = HttpOnlyPolicy.None,
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
        public async Task CookiePolicyCanHijackAppend()
        {
            var server = TestServer.Create(app =>
            {
                app.UseCookiePolicy(options => options.OnAppendCookie = ctx => ctx.CookieName = ctx.CookieValue = "Hao");
                app.Run(context =>
                {
                    context.Response.Cookies.Append("A", "A");
                    context.Response.Cookies.Append("B", "B", new CookieOptions { Secure = false });
                    context.Response.Cookies.Append("C", "C", new CookieOptions());
                    context.Response.Cookies.Append("D", "D", new CookieOptions { Secure = true });
                    return Task.FromResult(0);
                });
            });

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.NotNull(transaction.SetCookie);
            Assert.Equal("Hao=Hao; path=/", transaction.SetCookie[0]);
            Assert.Equal("Hao=Hao; path=/", transaction.SetCookie[1]);
            Assert.Equal("Hao=Hao; path=/", transaction.SetCookie[2]);
            Assert.Equal("Hao=Hao; path=/; secure", transaction.SetCookie[3]);
        }

        [Fact]
        public async Task CookiePolicyCanHijackDelete()
        {
            var server = TestServer.Create(app =>
            {
                app.UseCookiePolicy(options => options.OnDeleteCookie = ctx => ctx.CookieName = "A");
                app.Run(context =>
                {
                    context.Response.Cookies.Delete("A");
                    context.Response.Cookies.Delete("B", new CookieOptions { Secure = false });
                    context.Response.Cookies.Delete("C", new CookieOptions());
                    context.Response.Cookies.Delete("D", new CookieOptions { Secure = true });
                    return Task.FromResult(0);
                });
            });

            var transaction = await server.SendAsync("http://example.com/login");

            Assert.NotNull(transaction.SetCookie);
            Assert.Equal(1, transaction.SetCookie.Count);
            Assert.Equal("A=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/", transaction.SetCookie[0]);
        }

        [Fact]
        public async Task CookiePolicyCallsCookieFeature()
        {
            var server = TestServer.Create(app =>
            {
                app.Use(next => context =>
                {
                    context.Features.Set<IResponseCookiesFeature>(new TestCookieFeature());
                    return next(context);
                });
                app.UseCookiePolicy(options => options.OnDeleteCookie = ctx => ctx.CookieName = "A");
                app.Run(context =>
                {
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Delete("A"));
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Delete("A", new CookieOptions()));
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Append("A", "A"));
                    Assert.Throws<NotImplementedException>(() => context.Response.Cookies.Append("A", "A", new CookieOptions()));
                    return context.Response.WriteAsync("Done");
                });
            });

            var transaction = await server.SendAsync("http://example.com/login");
            Assert.Equal("Done", transaction.ResponseText);
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
            Action<CookiePolicyOptions> configureCookiePolicy,
            RequestDelegate configureSetup,
            params RequestTest[] tests)
        {
            var server = TestServer.Create(app =>
            {
                app.Map(path, map =>
                {
                    map.UseCookiePolicy(configureCookiePolicy);
                    map.Run(configureSetup);
                });
            });
            foreach (var test in tests)
            {
                await test.Execute(server);
            }
        }
    }
}