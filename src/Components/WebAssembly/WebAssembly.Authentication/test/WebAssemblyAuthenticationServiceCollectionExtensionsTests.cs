// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class WebAssemblyAuthenticationServiceCollectionExtensionsTests
    {
        [Fact]
        public void CanResolve_AccessTokenProvider()
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.AddApiAuthorization();
            var host = builder.Build();

            host.Services.GetRequiredService<IAccessTokenProvider>();
        }

        [Fact]
        public void CanResolve_IRemoteAuthenticationService()
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.AddApiAuthorization();
            var host = builder.Build();

            host.Services.GetRequiredService<IRemoteAuthenticationService<RemoteAuthenticationState>>();
        }

        [Fact]
        public void ApiAuthorizationOptions_ConfigurationDefaultsGetApplied()
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.AddApiAuthorization();
            var host = builder.Build();

            var options = host.Services.GetRequiredService<IOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

            var paths = options.Value.AuthenticationPaths;

            Assert.Equal("authentication/login", paths.LogInPath);
            Assert.Equal("authentication/login-callback", paths.LogInCallbackPath);
            Assert.Equal("authentication/login-failed", paths.LogInFailedPath);
            Assert.Equal("authentication/register", paths.RegisterPath);
            Assert.Equal("authentication/profile", paths.ProfilePath);
            Assert.Equal("Identity/Account/Register", paths.RemoteRegisterPath);
            Assert.Equal("Identity/Account/Manage", paths.RemoteProfilePath);
            Assert.Equal("authentication/logout", paths.LogOutPath);
            Assert.Equal("authentication/logout-callback", paths.LogOutCallbackPath);
            Assert.Equal("authentication/logout-failed", paths.LogOutFailedPath);
            Assert.Equal("authentication/logged-out", paths.LogOutSucceededPath);

            var user = options.Value.UserOptions;
            Assert.Equal("Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests", user.AuthenticationType);
            Assert.Equal("scope", user.ScopeClaim);
            Assert.Null(user.RoleClaim);
            Assert.Equal("name", user.NameClaim);

            Assert.Equal(
                "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
                options.Value.ProviderOptions.ConfigurationEndpoint);
        }

        [Fact]
        public void ApiAuthorizationOptions_DefaultsCanBeOverriden()
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.AddApiAuthorization(options =>
            {
                options.AuthenticationPaths = new RemoteAuthenticationApplicationPathsOptions
                {
                    LogInPath = "a",
                    LogInCallbackPath = "b",
                    LogInFailedPath = "c",
                    RegisterPath = "d",
                    ProfilePath = "e",
                    RemoteRegisterPath = "f",
                    RemoteProfilePath = "g",
                    LogOutPath = "h",
                    LogOutCallbackPath = "i",
                    LogOutFailedPath = "j",
                    LogOutSucceededPath = "k",
                };
                options.UserOptions = new RemoteAuthenticationUserOptions
                {
                    AuthenticationType = "l",
                    ScopeClaim = "m",
                    RoleClaim = "n",
                    NameClaim = "o",
                };
                options.ProviderOptions = new ApiAuthorizationProviderOptions
                {
                    ConfigurationEndpoint = "p"
                };
            });

            var host = builder.Build();

            var options = host.Services.GetRequiredService<IOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

            var paths = options.Value.AuthenticationPaths;

            Assert.Equal("a", paths.LogInPath);
            Assert.Equal("b", paths.LogInCallbackPath);
            Assert.Equal("c", paths.LogInFailedPath);
            Assert.Equal("d", paths.RegisterPath);
            Assert.Equal("e", paths.ProfilePath);
            Assert.Equal("f", paths.RemoteRegisterPath);
            Assert.Equal("g", paths.RemoteProfilePath);
            Assert.Equal("h", paths.LogOutPath);
            Assert.Equal("i", paths.LogOutCallbackPath);
            Assert.Equal("j", paths.LogOutFailedPath);
            Assert.Equal("k", paths.LogOutSucceededPath);

            var user = options.Value.UserOptions;
            Assert.Equal("l", user.AuthenticationType);
            Assert.Equal("m", user.ScopeClaim);
            Assert.Equal("n", user.RoleClaim);
            Assert.Equal("o", user.NameClaim);

            Assert.Equal("p", options.Value.ProviderOptions.ConfigurationEndpoint);
        }

        [Fact]
        public void OidcOptions_ConfigurationDefaultsGetApplied()
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.Replace(ServiceDescriptor.Singleton<NavigationManager, TestNavigationManager>());
            builder.Services.AddOidcAuthentication(options => { });
            var host = builder.Build();

            var options = host.Services.GetRequiredService<IOptions<RemoteAuthenticationOptions<OidcProviderOptions>>>();

            var paths = options.Value.AuthenticationPaths;

            Assert.Equal("authentication/login", paths.LogInPath);
            Assert.Equal("authentication/login-callback", paths.LogInCallbackPath);
            Assert.Equal("authentication/login-failed", paths.LogInFailedPath);
            Assert.Equal("authentication/register", paths.RegisterPath);
            Assert.Equal("authentication/profile", paths.ProfilePath);
            Assert.Null(paths.RemoteRegisterPath);
            Assert.Null(paths.RemoteProfilePath);
            Assert.Equal("authentication/logout", paths.LogOutPath);
            Assert.Equal("authentication/logout-callback", paths.LogOutCallbackPath);
            Assert.Equal("authentication/logout-failed", paths.LogOutFailedPath);
            Assert.Equal("authentication/logged-out", paths.LogOutSucceededPath);

            var user = options.Value.UserOptions;
            Assert.Null(user.AuthenticationType);
            Assert.Null(user.ScopeClaim);
            Assert.Null(user.RoleClaim);
            Assert.Equal("name", user.NameClaim);

            var provider = options.Value.ProviderOptions;
            Assert.Null(provider.Authority);
            Assert.Null(provider.ClientId);
            Assert.Equal(new[] { "openid", "profile" }, provider.DefaultScopes);
            Assert.Equal("https://www.example.com/base/authentication/login-callback", provider.RedirectUri);
            Assert.Equal("https://www.example.com/base/authentication/logout-callback", provider.PostLogoutRedirectUri);
        }

        [Fact]
        public void OidcOptions_DefaultsCanBeOverriden()
        {
            var builder = WebAssemblyHostBuilder.CreateDefault();
            builder.Services.AddOidcAuthentication(options =>
            {
                options.AuthenticationPaths = new RemoteAuthenticationApplicationPathsOptions
                {
                    LogInPath = "a",
                    LogInCallbackPath = "b",
                    LogInFailedPath = "c",
                    RegisterPath = "d",
                    ProfilePath = "e",
                    RemoteRegisterPath = "f",
                    RemoteProfilePath = "g",
                    LogOutPath = "h",
                    LogOutCallbackPath = "i",
                    LogOutFailedPath = "j",
                    LogOutSucceededPath = "k",
                };
                options.UserOptions = new RemoteAuthenticationUserOptions
                {
                    AuthenticationType = "l",
                    ScopeClaim = "m",
                    RoleClaim = "n",
                    NameClaim = "o",
                };
                options.ProviderOptions = new OidcProviderOptions
                {
                    Authority = "p",
                    ClientId = "q",
                    DefaultScopes = Array.Empty<string>(),
                    RedirectUri = "https://www.example.com/base/custom-login",
                    PostLogoutRedirectUri = "https://www.example.com/base/custom-logout",
                };
            });

            var host = builder.Build();

            var options = host.Services.GetRequiredService<IOptions<RemoteAuthenticationOptions<OidcProviderOptions>>>();

            var paths = options.Value.AuthenticationPaths;

            Assert.Equal("a", paths.LogInPath);
            Assert.Equal("b", paths.LogInCallbackPath);
            Assert.Equal("c", paths.LogInFailedPath);
            Assert.Equal("d", paths.RegisterPath);
            Assert.Equal("e", paths.ProfilePath);
            Assert.Equal("f", paths.RemoteRegisterPath);
            Assert.Equal("g", paths.RemoteProfilePath);
            Assert.Equal("h", paths.LogOutPath);
            Assert.Equal("i", paths.LogOutCallbackPath);
            Assert.Equal("j", paths.LogOutFailedPath);
            Assert.Equal("k", paths.LogOutSucceededPath);

            var user = options.Value.UserOptions;
            Assert.Equal("l", user.AuthenticationType);
            Assert.Equal("m", user.ScopeClaim);
            Assert.Equal("n", user.RoleClaim);
            Assert.Equal("o", user.NameClaim);

            var provider = options.Value.ProviderOptions;
            Assert.Equal("p", provider.Authority);
            Assert.Equal("q", provider.ClientId);
            Assert.Equal(Array.Empty<string>(), provider.DefaultScopes);
            Assert.Equal("https://www.example.com/base/custom-login", provider.RedirectUri);
            Assert.Equal("https://www.example.com/base/custom-logout", provider.PostLogoutRedirectUri);
        }

        private class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("https://www.example.com/base/", "https://www.example.com/base/counter");
            }

            protected override void NavigateToCore(string uri, bool forceLoad) => throw new System.NotImplementedException();
        }
    }
}
