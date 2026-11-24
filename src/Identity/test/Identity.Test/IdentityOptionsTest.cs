// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Test;

public class IdentityOptionsTest
{
    [Fact]
    public void VerifyDefaultOptions()
    {
        var options = new IdentityOptions();
        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Lockout.DefaultLockoutTimeSpan);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);

        Assert.True(options.Password.RequireDigit);
        Assert.True(options.Password.RequireLowercase);
        Assert.True(options.Password.RequireNonAlphanumeric);
        Assert.True(options.Password.RequireUppercase);
        Assert.Equal(6, options.Password.RequiredLength);
        Assert.Equal(1, options.Password.RequiredUniqueChars);

        Assert.Equal("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+", options.User.AllowedUserNameCharacters);
        Assert.False(options.User.RequireUniqueEmail);

        Assert.Equal(ClaimTypes.Role, options.ClaimsIdentity.RoleClaimType);
        Assert.Equal(ClaimTypes.Name, options.ClaimsIdentity.UserNameClaimType);
        Assert.Equal(ClaimTypes.NameIdentifier, options.ClaimsIdentity.UserIdClaimType);
        Assert.Equal("AspNet.Identity.SecurityStamp", options.ClaimsIdentity.SecurityStampClaimType);
    }

    [Fact]
    public void CanCustomizeIdentityOptions()
    {
        var services = new ServiceCollection().Configure<IdentityOptions>(options => options.Password.RequiredLength = -1);
        services.AddIdentity<PocoUser, PocoRole>();
        var serviceProvider = services.BuildServiceProvider();

        var setup = serviceProvider.GetRequiredService<IConfigureOptions<IdentityOptions>>();
        Assert.NotNull(setup);
        var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
        Assert.NotNull(optionsGetter);
        var myOptions = optionsGetter.Value;
        Assert.True(myOptions.Password.RequireLowercase);
        Assert.True(myOptions.Password.RequireDigit);
        Assert.True(myOptions.Password.RequireNonAlphanumeric);
        Assert.True(myOptions.Password.RequireUppercase);
        Assert.Equal(1, myOptions.Password.RequiredUniqueChars);
        Assert.Equal(-1, myOptions.Password.RequiredLength);
    }

    [Fact]
    public void CanSetupIdentityOptions()
    {
        var services = new ServiceCollection();
        services.AddIdentity<PocoUser, PocoRole>(options => options.User.RequireUniqueEmail = true);
        var serviceProvider = services.BuildServiceProvider();

        var optionsGetter = serviceProvider.GetRequiredService<IOptions<IdentityOptions>>();
        Assert.NotNull(optionsGetter);

        var myOptions = optionsGetter.Value;
        Assert.True(myOptions.User.RequireUniqueEmail);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task PasskeyCookieInheritsTwoFactorConfiguration(int registration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (registration is 0)
        {
            services.AddAuthentication().AddTwoFactorUserIdCookie();
        }
        else if (registration is 1)
        {
            services.AddAuthentication().AddIdentityCookies();
        }
        else
        {
            services.AddIdentity<PocoUser, PocoRole>();
        }
        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.TwoFactorUserIdScheme, options =>
        {
            options.Cookie.Name = "custom-two-factor";
            options.Cookie.Path = "/identity";
            options.Cookie.Domain = "example.com";
            options.Cookie.Extensions.Add("custom=value");
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
        });
        using var provider = services.BuildServiceProvider();
        var schemes = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        Assert.NotNull(await schemes.GetSchemeAsync(IdentityConstants.PasskeyScheme));
        var monitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var twoFactor = monitor.Get(IdentityConstants.TwoFactorUserIdScheme);
        var passkey = monitor.Get(IdentityConstants.PasskeyScheme);
        Assert.Equal("custom-two-factor.Passkey", passkey.Cookie.Name);
        Assert.Equal("/identity", passkey.Cookie.Path);
        Assert.Equal("example.com", passkey.Cookie.Domain);
        Assert.Equal(twoFactor.Cookie.Extensions, passkey.Cookie.Extensions);
        Assert.NotSame(twoFactor.Cookie, passkey.Cookie);
        Assert.Equal(TimeSpan.FromMinutes(30), passkey.ExpireTimeSpan);
        Assert.False(passkey.SlidingExpiration);
        Assert.NotSame(twoFactor.Events, passkey.Events);
        Assert.NotSame(twoFactor.TicketDataFormat, passkey.TicketDataFormat);
    }
    [Fact]
    public void CanConfigureCookieOptions()
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddIdentityCookies(o =>
        {
            o.ApplicationCookie.Configure(a => a.Cookie.Name = "a");
            o.ExternalCookie.Configure(a => a.Cookie.Name = "b");
            o.TwoFactorRememberMeCookie.Configure(a => a.Cookie.Name = "c");
            o.TwoFactorUserIdCookie.Configure(a => a.Cookie.Name = "d");
        });
        services.Configure<CookieAuthenticationOptions>("Identity.Passkey", a => a.Cookie.Name = "e");
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        Assert.NotNull(options);

        Assert.Equal("a", options.Get(IdentityConstants.ApplicationScheme).Cookie.Name);
        Assert.Equal("b", options.Get(IdentityConstants.ExternalScheme).Cookie.Name);
        Assert.Equal("c", options.Get(IdentityConstants.TwoFactorRememberMeScheme).Cookie.Name);
        Assert.Equal("d", options.Get(IdentityConstants.TwoFactorUserIdScheme).Cookie.Name);
        Assert.Equal("e", options.Get(IdentityConstants.PasskeyScheme).Cookie.Name);
    }
}
