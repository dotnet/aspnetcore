
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Core.Test;

public class AuthenticationSchemeProviderTests
{
    [Fact]
    public async Task NoDefaultsWithoutAutoDefaultScheme()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.DisableAutoDefaultScheme = true;
            o.AddScheme<SignInHandler>("B", "whatever");
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        await VerifyAllDefaults(provider, null);
    }

    [Fact]
    public async Task NoDefaultsWithMoreSchemes()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<SignInHandler>("A", "whatever");
            o.AddScheme<SignInHandler>("B", "whatever");
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        await VerifyAllDefaults(provider, null);
    }

    [Fact]
    public async Task DefaultSchemesUsesSingleScheme()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<SignInHandler>("B", "whatever");
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal("B", (await provider.GetDefaultForbidSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultAuthenticateSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultChallengeSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultSignInSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultSignOutSchemeAsync())!.Name);
    }

    [Fact]
    public async Task DefaultSchemesFallbackToDefaultScheme()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.DefaultScheme = "B";
            o.AddScheme<SignInHandler>("A", "whatever");
            o.AddScheme<SignInHandler>("B", "whatever");
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal("B", (await provider.GetDefaultForbidSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultAuthenticateSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultChallengeSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultSignInSchemeAsync())!.Name);
        Assert.Equal("B", (await provider.GetDefaultSignOutSchemeAsync())!.Name);
    }

    [Fact]
    public async Task DefaultSignOutFallsbackToSignIn()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<SignInHandler>("signin", "whatever");
            o.AddScheme<Handler>("foobly", "whatever");
            o.DefaultSignInScheme = "signin";
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await provider.GetDefaultSignOutSchemeAsync();
        Assert.NotNull(scheme);
        Assert.Equal("signin", scheme!.Name);
    }

    [Fact]
    public async Task DefaultForbidFallsbackToChallenge()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<Handler>("challenge", "whatever");
            o.AddScheme<Handler>("foobly", "whatever");
            o.DefaultChallengeScheme = "challenge";
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await provider.GetDefaultForbidSchemeAsync();
        Assert.NotNull(scheme);
        Assert.Equal("challenge", scheme!.Name);
    }

    [Fact]
    public async Task DefaultSchemesAreSet()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<SignInHandler>("A", "whatever");
            o.AddScheme<SignInHandler>("B", "whatever");
            o.AddScheme<SignInHandler>("C", "whatever");
            o.AddScheme<SignInHandler>("Def", "whatever");
            o.DefaultScheme = "Def";
            o.DefaultChallengeScheme = "A";
            o.DefaultForbidScheme = "B";
            o.DefaultSignInScheme = "C";
            o.DefaultSignOutScheme = "A";
            o.DefaultAuthenticateScheme = "C";
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.Equal("B", (await provider.GetDefaultForbidSchemeAsync())!.Name);
        Assert.Equal("C", (await provider.GetDefaultAuthenticateSchemeAsync())!.Name);
        Assert.Equal("A", (await provider.GetDefaultChallengeSchemeAsync())!.Name);
        Assert.Equal("C", (await provider.GetDefaultSignInSchemeAsync())!.Name);
        Assert.Equal("A", (await provider.GetDefaultSignOutSchemeAsync())!.Name);
    }

    [Fact]
    public async Task SignOutWillDefaultsToSignInThatDoesNotSignOut()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<Handler>("signin", "whatever");
            o.DefaultSignInScheme = "signin";
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.NotNull(await provider.GetDefaultSignOutSchemeAsync());
    }

    [Fact]
    public void SchemeRegistrationIsCaseSensitive()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
            o.AddScheme<Handler>("signin", "whatever");
            o.AddScheme<Handler>("signin", "whatever");
        }).BuildServiceProvider();

        var error = Assert.Throws<InvalidOperationException>(() => services.GetRequiredService<IAuthenticationSchemeProvider>());

        Assert.Contains("Scheme already exists: signin", error.Message);
    }

    [Fact]
    public void CanSafelyTryAddSchemes()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
        }).BuildServiceProvider();

        var o = services.GetRequiredService<IAuthenticationSchemeProvider>();
        Assert.True(o.TryAddScheme(new AuthenticationScheme("signin", "whatever", typeof(Handler))));
        Assert.True(o.TryAddScheme(new AuthenticationScheme("signin2", "whatever", typeof(Handler))));
        Assert.False(o.TryAddScheme(new AuthenticationScheme("signin", "whatever", typeof(Handler))));
        Assert.True(o.TryAddScheme(new AuthenticationScheme("signin3", "whatever", typeof(Handler))));
        Assert.False(o.TryAddScheme(new AuthenticationScheme("signin2", "whatever", typeof(Handler))));
        o.RemoveScheme("signin2");
        Assert.True(o.TryAddScheme(new AuthenticationScheme("signin2", "whatever", typeof(Handler))));
    }

    [Fact]
    public async Task LookupUsesProvidedStringComparer()
    {
        var services = new ServiceCollection().AddOptions()
            .AddSingleton<IAuthenticationSchemeProvider, IgnoreCaseSchemeProvider>()
            .AddAuthenticationCore(o => o.AddScheme<Handler>("signin", "whatever"))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();

        var a = await provider.GetSchemeAsync("signin");
        var b = await provider.GetSchemeAsync("SignIn");
        var c = await provider.GetSchemeAsync("SIGNIN");

        Assert.NotNull(a);
        Assert.Same(a, b);
        Assert.Same(b, c);
    }

    [Fact]
    public async Task AutoDefaultSchemeAddRemoveWorks()
    {
        var services = new ServiceCollection().AddOptions().AddAuthenticationCore(o =>
        {
        }).BuildServiceProvider();

        var provider = services.GetRequiredService<IAuthenticationSchemeProvider>();

        var scheme1 = new AuthenticationScheme("signin1", "whatever", typeof(Handler));
        var scheme2 = new AuthenticationScheme("signin2", "whatever", typeof(Handler));
        var scheme3 = new AuthenticationScheme("signin3", "whatever", typeof(Handler));

        // No schemes, so null default
        await VerifyAllDefaults(provider, null);

        // One scheme, should be default
        Assert.True(provider.TryAddScheme(scheme1));
        await VerifyAllDefaults(provider, scheme1);

        // Still one scheme, should be default
        Assert.False(provider.TryAddScheme(scheme1));
        await VerifyAllDefaults(provider, scheme1);

        // Two schemes, should be null
        Assert.True(provider.TryAddScheme(scheme2));
        await VerifyAllDefaults(provider, null);

        // Three schemes, should be null
        Assert.True(provider.TryAddScheme(scheme3));
        await VerifyAllDefaults(provider, null);

        // Remove one scheme, still two schemes, should be null
        provider.RemoveScheme(scheme2.Name);
        await VerifyAllDefaults(provider, null);

        // Remove same scheme, still two schemes, should be null
        provider.RemoveScheme(scheme2.Name);
        await VerifyAllDefaults(provider, null);

        // Remove a scheme, now should have a default single
        provider.RemoveScheme(scheme1.Name);
        await VerifyAllDefaults(provider, scheme3);

        // Remove last scheme, should be no default
        provider.RemoveScheme(scheme3.Name);
        await VerifyAllDefaults(provider, null);

        // Add a scheme again, should be default
        Assert.True(provider.TryAddScheme(scheme2));
        await VerifyAllDefaults(provider, scheme2);
    }

    private async Task VerifyAllDefaults(IAuthenticationSchemeProvider provider, AuthenticationScheme? expected)
    {
        Assert.Equal(await provider.GetDefaultForbidSchemeAsync(), expected);
        Assert.Equal(await provider.GetDefaultAuthenticateSchemeAsync(), expected);
        Assert.Equal(await provider.GetDefaultChallengeSchemeAsync(), expected);
        Assert.Equal(await provider.GetDefaultSignInSchemeAsync(), expected);
        Assert.Equal(await provider.GetDefaultSignOutSchemeAsync(), expected);
    }

    private class Handler : IAuthenticationHandler
    {
        public Task<AuthenticateResult> AuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }

        public Task ForbidAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class SignInHandler : Handler, IAuthenticationSignInHandler
    {
        public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }

        public Task SignOutAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }
    }

    private class SignOutHandler : Handler, IAuthenticationSignOutHandler
    {
        public Task SignOutAsync(AuthenticationProperties? properties)
        {
            throw new NotImplementedException();
        }
    }

    private class IgnoreCaseSchemeProvider : AuthenticationSchemeProvider
    {
        public IgnoreCaseSchemeProvider(IOptions<AuthenticationOptions> options)
            : base(options, new Dictionary<string, AuthenticationScheme>(StringComparer.OrdinalIgnoreCase))
        {
        }
    }
}
