// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public abstract class SharedAuthenticationTests<TOptions> where TOptions : AuthenticationSchemeOptions
    {
        protected TestClock Clock { get; } = new TestClock();

        protected abstract string DefaultScheme { get; }
        protected virtual string DisplayName { get; }
        protected abstract Type HandlerType { get; }

        protected virtual bool SupportsSignIn { get => true; }
        protected virtual bool SupportsSignOut { get => true; }

        protected abstract void RegisterAuth(AuthenticationBuilder services, Action<TOptions> configure);

        [Fact]
        public async Task CanForwardDefault()
        {
            var services = new ServiceCollection().AddLogging();

            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler>("auth1", "auth1");
            });
            RegisterAuth(builder, o => o.ForwardDefault = "auth1");

            var forwardDefault = new TestHandler();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);

            await context.AuthenticateAsync();
            Assert.Equal(1, forwardDefault.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, forwardDefault.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, forwardDefault.ChallengeCount);

            if (SupportsSignOut)
            {
                await context.SignOutAsync();
                Assert.Equal(1, forwardDefault.SignOutCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            }

            if (SupportsSignIn)
            {
                await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
                Assert.Equal(1, forwardDefault.SignInCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
            }
        }

        [Fact]
        public async Task ForwardSignInWinsOverDefault()
        {
            if (SupportsSignIn)
            {
                var services = new ServiceCollection().AddLogging();

                var builder = services.AddAuthentication(o =>
                {
                    o.DefaultScheme = DefaultScheme;
                    o.AddScheme<TestHandler2>("auth1", "auth1");
                    o.AddScheme<TestHandler>("specific", "specific");
                });
                RegisterAuth(builder, o =>
                {
                    o.ForwardDefault = "auth1";
                    o.ForwardSignIn = "specific";
                });

                var specific = new TestHandler();
                services.AddSingleton(specific);
                var forwardDefault = new TestHandler2();
                services.AddSingleton(forwardDefault);

                var sp = services.BuildServiceProvider();
                var context = new DefaultHttpContext();
                context.RequestServices = sp;

                await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
                Assert.Equal(1, specific.SignInCount);
                Assert.Equal(0, specific.AuthenticateCount);
                Assert.Equal(0, specific.ForbidCount);
                Assert.Equal(0, specific.ChallengeCount);
                Assert.Equal(0, specific.SignOutCount);

                Assert.Equal(0, forwardDefault.AuthenticateCount);
                Assert.Equal(0, forwardDefault.ForbidCount);
                Assert.Equal(0, forwardDefault.ChallengeCount);
                Assert.Equal(0, forwardDefault.SignInCount);
                Assert.Equal(0, forwardDefault.SignOutCount);
            }
        }

        [Fact]
        public async Task ForwardSignOutWinsOverDefault()
        {
            if (SupportsSignOut)
            {
                var services = new ServiceCollection().AddLogging();
                var builder = services.AddAuthentication(o =>
                {
                    o.DefaultScheme = DefaultScheme;
                    o.AddScheme<TestHandler2>("auth1", "auth1");
                    o.AddScheme<TestHandler>("specific", "specific");
                });
                RegisterAuth(builder, o =>
                {
                    o.ForwardDefault = "auth1";
                    o.ForwardSignOut = "specific";
                });

                var specific = new TestHandler();
                services.AddSingleton(specific);
                var forwardDefault = new TestHandler2();
                services.AddSingleton(forwardDefault);

                var sp = services.BuildServiceProvider();
                var context = new DefaultHttpContext();
                context.RequestServices = sp;

                await context.SignOutAsync();
                Assert.Equal(1, specific.SignOutCount);
                Assert.Equal(0, specific.AuthenticateCount);
                Assert.Equal(0, specific.ForbidCount);
                Assert.Equal(0, specific.ChallengeCount);
                Assert.Equal(0, specific.SignInCount);

                Assert.Equal(0, forwardDefault.AuthenticateCount);
                Assert.Equal(0, forwardDefault.ForbidCount);
                Assert.Equal(0, forwardDefault.ChallengeCount);
                Assert.Equal(0, forwardDefault.SignInCount);
                Assert.Equal(0, forwardDefault.SignOutCount);
            }
        }

        [Fact]
        public async Task ForwardForbidWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardForbid = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.ForbidAsync();
            Assert.Equal(0, specific.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(1, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
        }

        private class RunOnce : IClaimsTransformation
        {
            public int Ran = 0;
            public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
            {
                Ran++;
                return Task.FromResult(new ClaimsPrincipal());
            }
        }

        [Fact]
        public async Task ForwardAuthenticateOnlyRunsTransformOnceByDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var transform = new RunOnce();
            var builder = services.AddSingleton<IClaimsTransformation>(transform).AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardAuthenticate = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, transform.Ran);
        }

        [Fact]
        public async Task ForwardAuthenticateWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardAuthenticate = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(0, specific.SignOutCount);
            Assert.Equal(1, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
        }

        [Fact]
        public async Task ForwardChallengeWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardChallenge = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.ChallengeAsync();
            Assert.Equal(0, specific.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(1, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
        }

        [Fact]
        public async Task ForwardSelectorWinsOverDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardDefaultSelector = _ => "selector";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);
            var selector = new TestHandler3();
            services.AddSingleton(selector);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, selector.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, selector.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, selector.ChallengeCount);

            if (SupportsSignOut)
            {
                await context.SignOutAsync();
                Assert.Equal(1, selector.SignOutCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            }

            if (SupportsSignIn)
            {
                await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
                Assert.Equal(1, selector.SignInCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
            }

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);
            Assert.Equal(0, specific.SignOutCount);
        }

        [Fact]
        public async Task NullForwardSelectorUsesDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardDefaultSelector = _ => null;
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);
            var selector = new TestHandler3();
            services.AddSingleton(selector);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, forwardDefault.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, forwardDefault.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, forwardDefault.ChallengeCount);

            if (SupportsSignOut)
            {
                await context.SignOutAsync();
                Assert.Equal(1, forwardDefault.SignOutCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            }

            if (SupportsSignIn)
            {
                await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
                Assert.Equal(1, forwardDefault.SignInCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
            }

            Assert.Equal(0, selector.AuthenticateCount);
            Assert.Equal(0, selector.ForbidCount);
            Assert.Equal(0, selector.ChallengeCount);
            Assert.Equal(0, selector.SignInCount);
            Assert.Equal(0, selector.SignOutCount);
            Assert.Equal(0, specific.AuthenticateCount);
            Assert.Equal(0, specific.ForbidCount);
            Assert.Equal(0, specific.ChallengeCount);
            Assert.Equal(0, specific.SignInCount);
            Assert.Equal(0, specific.SignOutCount);
        }

        [Fact]
        public async Task SpecificForwardWinsOverSelectorAndDefault()
        {
            var services = new ServiceCollection().AddLogging();
            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = DefaultScheme;
                o.AddScheme<TestHandler2>("auth1", "auth1");
                o.AddScheme<TestHandler3>("selector", "selector");
                o.AddScheme<TestHandler>("specific", "specific");
            });
            RegisterAuth(builder, o =>
            {
                o.ForwardDefault = "auth1";
                o.ForwardDefaultSelector = _ => "selector";
                o.ForwardAuthenticate = "specific";
                o.ForwardChallenge = "specific";
                o.ForwardSignIn = "specific";
                o.ForwardSignOut = "specific";
                o.ForwardForbid = "specific";
            });

            var specific = new TestHandler();
            services.AddSingleton(specific);
            var forwardDefault = new TestHandler2();
            services.AddSingleton(forwardDefault);
            var selector = new TestHandler3();
            services.AddSingleton(selector);

            var sp = services.BuildServiceProvider();
            var context = new DefaultHttpContext();
            context.RequestServices = sp;

            await context.AuthenticateAsync();
            Assert.Equal(1, specific.AuthenticateCount);

            await context.ForbidAsync();
            Assert.Equal(1, specific.ForbidCount);

            await context.ChallengeAsync();
            Assert.Equal(1, specific.ChallengeCount);

            if (SupportsSignOut)
            {
                await context.SignOutAsync();
                Assert.Equal(1, specific.SignOutCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            }

            if (SupportsSignIn)
            {
                await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity("whatever")));
                Assert.Equal(1, specific.SignInCount);
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
            }

            Assert.Equal(0, forwardDefault.AuthenticateCount);
            Assert.Equal(0, forwardDefault.ForbidCount);
            Assert.Equal(0, forwardDefault.ChallengeCount);
            Assert.Equal(0, forwardDefault.SignInCount);
            Assert.Equal(0, forwardDefault.SignOutCount);
            Assert.Equal(0, selector.AuthenticateCount);
            Assert.Equal(0, selector.ForbidCount);
            Assert.Equal(0, selector.ChallengeCount);
            Assert.Equal(0, selector.SignInCount);
            Assert.Equal(0, selector.SignOutCount);
        }

        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection();
            var builder = services.AddAuthentication();
            RegisterAuth(builder, o => { });
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(DefaultScheme);
            Assert.NotNull(scheme);
            Assert.Equal(HandlerType, scheme.HandlerType);
            Assert.Equal(DisplayName, scheme.DisplayName);
        }
    }
}
