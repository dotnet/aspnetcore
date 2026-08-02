// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class WebAssemblyAuthenticationServiceCollectionExtensionsTests
{
    [Fact]
    public void CanResolve_AccessTokenProvider()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddApiAuthorization();
        var host = builder.Build();

        host.Services.GetRequiredService<IAccessTokenProvider>();
    }

    [Fact]
    public void CanResolve_IRemoteAuthenticationService()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddApiAuthorization();
        var host = builder.Build();

        host.Services.GetRequiredService<IRemoteAuthenticationService<RemoteAuthenticationState>>();
    }

    [Fact]
    public void ApiAuthorizationOptions_ConfigurationDefaultsGetApplied()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddApiAuthorization();
        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

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
        Assert.Equal("role", user.RoleClaim);
        Assert.Equal("name", user.NameClaim);

        Assert.Equal(
            "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
            options.Value.ProviderOptions.ConfigurationEndpoint);
    }

    [Fact]
    public void ApiAuthorizationOptionsConfigurationCallback_GetsCalledOnce()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var calls = 0;
        builder.Services.AddApiAuthorization(options =>
        {
            calls++;
        });

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

        var user = options.Value.UserOptions;
        Assert.Equal("Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests", user.AuthenticationType);

        // Make sure that the defaults are applied on this overload
        Assert.Equal("role", user.RoleClaim);

        Assert.Equal(
            "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
            options.Value.ProviderOptions.ConfigurationEndpoint);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void ApiAuthorizationTestAuthenticationState_SetsUpConfiguration()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var calls = 0;
        builder.Services.AddApiAuthorization<TestAuthenticationState>(options => calls++);

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

        var user = options.Value.UserOptions;
        // Make sure that the defaults are applied on this overload
        Assert.Equal("role", user.RoleClaim);

        Assert.Equal(
            "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
            options.Value.ProviderOptions.ConfigurationEndpoint);

        var authenticationService = host.Services.GetService<IRemoteAuthenticationService<TestAuthenticationState>>();
        Assert.NotNull(authenticationService);
        Assert.IsType<RemoteAuthenticationService<TestAuthenticationState, RemoteUserAccount, ApiAuthorizationProviderOptions>>(authenticationService);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void ApiAuthorizationTestAuthenticationState_NoCallback_SetsUpConfiguration()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddApiAuthorization<TestAuthenticationState>();

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

        var user = options.Value.UserOptions;
        // Make sure that the defaults are applied on this overload
        Assert.Equal("role", user.RoleClaim);

        Assert.Equal(
            "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
            options.Value.ProviderOptions.ConfigurationEndpoint);

        var authenticationService = host.Services.GetService<IRemoteAuthenticationService<TestAuthenticationState>>();
        Assert.NotNull(authenticationService);
        Assert.IsType<RemoteAuthenticationService<TestAuthenticationState, RemoteUserAccount, ApiAuthorizationProviderOptions>>(authenticationService);
    }

    [Fact]
    public void ApiAuthorizationCustomAuthenticationStateAndAccount_SetsUpConfiguration()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var calls = 0;
        builder.Services.AddApiAuthorization<TestAuthenticationState, TestAccount>(options => calls++);

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

        var user = options.Value.UserOptions;
        // Make sure that the defaults are applied on this overload
        Assert.Equal("role", user.RoleClaim);

        Assert.Equal(
            "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
            options.Value.ProviderOptions.ConfigurationEndpoint);

        var authenticationService = host.Services.GetService<IRemoteAuthenticationService<TestAuthenticationState>>();
        Assert.NotNull(authenticationService);
        Assert.IsType<RemoteAuthenticationService<TestAuthenticationState, TestAccount, ApiAuthorizationProviderOptions>>(authenticationService);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void ApiAuthorizationTestAuthenticationStateAndAccount_NoCallback_SetsUpConfiguration()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddApiAuthorization<TestAuthenticationState, TestAccount>();

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

        var user = options.Value.UserOptions;
        // Make sure that the defaults are applied on this overload
        Assert.Equal("role", user.RoleClaim);

        Assert.Equal(
            "_configuration/Microsoft.AspNetCore.Components.WebAssembly.Authentication.Tests",
            options.Value.ProviderOptions.ConfigurationEndpoint);

        var authenticationService = host.Services.GetService<IRemoteAuthenticationService<TestAuthenticationState>>();
        Assert.NotNull(authenticationService);
        Assert.IsType<RemoteAuthenticationService<TestAuthenticationState, TestAccount, ApiAuthorizationProviderOptions>>(authenticationService);
    }

    [Fact]
    public void ApiAuthorizationOptions_DefaultsCanBeOverriden()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddApiAuthorization(options =>
        {
            options.AuthenticationPaths.LogInPath = "a";
            options.AuthenticationPaths.LogInCallbackPath = "b";
            options.AuthenticationPaths.LogInFailedPath = "c";
            options.AuthenticationPaths.RegisterPath = "d";
            options.AuthenticationPaths.ProfilePath = "e";
            options.AuthenticationPaths.RemoteRegisterPath = "f";
            options.AuthenticationPaths.RemoteProfilePath = "g";
            options.AuthenticationPaths.LogOutPath = "h";
            options.AuthenticationPaths.LogOutCallbackPath = "i";
            options.AuthenticationPaths.LogOutFailedPath = "j";
            options.AuthenticationPaths.LogOutSucceededPath = "k";
            options.UserOptions.AuthenticationType = "l";
            options.UserOptions.ScopeClaim = "m";
            options.UserOptions.RoleClaim = "n";
            options.UserOptions.NameClaim = "o";
            options.ProviderOptions.ConfigurationEndpoint = "p";
        });

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>>();

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
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.Replace(ServiceDescriptor.Singleton<NavigationManager, TestNavigationManager>());
        builder.Services.AddOidcAuthentication(options => { });
        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();

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
        Assert.Equal(new Dictionary<string, string>(), provider.AdditionalProviderParameters);
        Assert.Equal("https://www.example.com/base/authentication/login-callback", provider.RedirectUri);
        Assert.Equal("https://www.example.com/base/authentication/logout-callback", provider.PostLogoutRedirectUri);
    }

    [Fact]
    public void OidcOptions_DefaultsCanBeOverriden()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddOidcAuthentication(options =>
        {
            options.AuthenticationPaths.LogInPath = "a";
            options.AuthenticationPaths.LogInCallbackPath = "b";
            options.AuthenticationPaths.LogInFailedPath = "c";
            options.AuthenticationPaths.RegisterPath = "d";
            options.AuthenticationPaths.ProfilePath = "e";
            options.AuthenticationPaths.RemoteRegisterPath = "f";
            options.AuthenticationPaths.RemoteProfilePath = "g";
            options.AuthenticationPaths.LogOutPath = "h";
            options.AuthenticationPaths.LogOutCallbackPath = "i";
            options.AuthenticationPaths.LogOutFailedPath = "j";
            options.AuthenticationPaths.LogOutSucceededPath = "k";
            options.UserOptions.AuthenticationType = "l";
            options.UserOptions.ScopeClaim = "m";
            options.UserOptions.RoleClaim = "n";
            options.UserOptions.NameClaim = "o";
            options.ProviderOptions.Authority = "p";
            options.ProviderOptions.ClientId = "q";
            options.ProviderOptions.DefaultScopes.Clear();
            options.ProviderOptions.AdditionalProviderParameters.Add("r", "s");
            options.ProviderOptions.RedirectUri = "https://www.example.com/base/custom-login";
            options.ProviderOptions.PostLogoutRedirectUri = "https://www.example.com/base/custom-logout";
        });

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();

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
        Assert.Equal(new Dictionary<string, string>() { { "r", "s" } }, provider.AdditionalProviderParameters);
        Assert.Equal("https://www.example.com/base/custom-login", provider.RedirectUri);
        Assert.Equal("https://www.example.com/base/custom-logout", provider.PostLogoutRedirectUri);
    }

    [Fact]
    public void AddOidc_ConfigurationGetsCalledOnce()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var calls = 0;

        builder.Services.AddOidcAuthentication(options => calls++);
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(NavigationManager), new TestNavigationManager()));

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        Assert.Equal("name", options.Value.UserOptions.NameClaim);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void AddOidc_CustomState_SetsUpConfiguration()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var calls = 0;

        builder.Services.AddOidcAuthentication<TestAuthenticationState>(options => options.ProviderOptions.Authority = (++calls).ToString(CultureInfo.InvariantCulture));
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(NavigationManager), new TestNavigationManager()));

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        // Make sure options are applied
        Assert.Equal("name", options.Value.UserOptions.NameClaim);

        Assert.Equal("1", options.Value.ProviderOptions.Authority);

        var authenticationService = host.Services.GetService<IRemoteAuthenticationService<TestAuthenticationState>>();
        Assert.NotNull(authenticationService);
        Assert.IsType<RemoteAuthenticationService<TestAuthenticationState, RemoteUserAccount, OidcProviderOptions>>(authenticationService);
    }

    [Fact]
    public void AddOidc_CustomStateAndAccount_SetsUpConfiguration()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var calls = 0;

        builder.Services.AddOidcAuthentication<TestAuthenticationState, TestAccount>(options => options.ProviderOptions.Authority = (++calls).ToString(CultureInfo.InvariantCulture));
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(NavigationManager), new TestNavigationManager()));

        var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();
        // Make sure options are applied
        Assert.Equal("name", options.Value.UserOptions.NameClaim);

        Assert.Equal("1", options.Value.ProviderOptions.Authority);

        var authenticationService = host.Services.GetService<IRemoteAuthenticationService<TestAuthenticationState>>();
        Assert.NotNull(authenticationService);
        Assert.IsType<RemoteAuthenticationService<TestAuthenticationState, TestAccount, OidcProviderOptions>>(authenticationService);
    }

    [Fact]
    public void OidcProviderOptionsAndDependencies_NotResolvedFromRootScope()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        var calls = 0;

        builder.Services.AddOidcAuthentication<TestAuthenticationState, TestAccount>(options => { });
        builder.Services.Replace(ServiceDescriptor.Scoped(typeof(NavigationManager), _ =>
        {
            calls++;
            return new TestNavigationManager();
        }));

        var host = builder.Build();

        using var scope = host.Services.CreateScope();

        // from the root scope.
        var rootOptions = host.Services.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();

        // from the created scope
        var scopedOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<RemoteAuthenticationOptions<OidcProviderOptions>>>();

        // we should have 2 navigation managers. One in the root scope, and one in the created scope.
        Assert.Equal(2, calls);
    }

    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://www.example.com/base/", "https://www.example.com/base/counter");
        }

        protected override void NavigateToCore(string uri, bool forceLoad) => throw new System.NotImplementedException();
    }

    private class TestAuthenticationState : RemoteAuthenticationState
    {
    }

    private class TestAccount : RemoteUserAccount
    {
    }
}
