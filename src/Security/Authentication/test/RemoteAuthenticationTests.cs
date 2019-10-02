// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public abstract class RemoteAuthenticationTests<TOptions> : SharedAuthenticationTests<TOptions> where TOptions : RemoteAuthenticationOptions
    {
        protected override string DisplayName => DefaultScheme;

        private TestServer CreateServer(Action<TOptions> configureOptions, Func<HttpContext, Task> testpath = null, bool isDefault = true)
            => CreateServerWithServices(s =>
            {
                var builder = s.AddAuthentication();
                if (isDefault)
                {
                    s.Configure<AuthenticationOptions>(o => o.DefaultScheme = DefaultScheme);
                }
                RegisterAuth(builder, configureOptions);
                s.AddSingleton<ISystemClock>(Clock);
            }, testpath);


        protected virtual TestServer CreateServerWithServices(Action<IServiceCollection> configureServices, Func<HttpContext, Task> testpath = null)
        {
            //private static TestServer CreateServer(Action<IApplicationBuilder> configure, Action<IServiceCollection> configureServices, Func<HttpContext, Task<bool>> handler)
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        if (testpath != null)
                        {
                            await testpath(context);
                        }
                        await next();
                    });
                })
                .ConfigureServices(configureServices);
            return new TestServer(builder);
        }

        protected abstract void ConfigureDefaults(TOptions o);

        [Fact]
        public async Task VerifySignInSchemeCannotBeSetToSelf()
        {
            var server = CreateServer(
                o => 
                {
                    ConfigureDefaults(o);
                    o.SignInScheme = DefaultScheme;
                },
                context => context.ChallengeAsync(DefaultScheme));
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("https://example.com/challenge"));
            Assert.Contains("cannot be set to itself", error.Message);
        }

        [Fact]
        public async Task VerifySignInSchemeCannotBeSetToSelfUsingDefaultScheme()
        {
            var server = CreateServer(
                o => o.SignInScheme = null,
                context => context.ChallengeAsync(DefaultScheme),
                isDefault: true);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("https://example.com/challenge"));
            Assert.Contains("cannot be set to itself", error.Message);
        }

        [Fact]
        public async Task VerifySignInSchemeCannotBeSetToSelfUsingDefaultSignInScheme()
        {
            var server = CreateServerWithServices(
                services =>
                {
                    var builder = services.AddAuthentication(o => o.DefaultSignInScheme = DefaultScheme);
                    RegisterAuth(builder, o => o.SignInScheme = null);
                },
                context => context.ChallengeAsync(DefaultScheme));
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync("https://example.com/challenge"));
            Assert.Contains("cannot be set to itself", error.Message);
        }
    }
}
