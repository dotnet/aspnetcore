// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscCookieProtectionTests
{
    private const string SourceScheme = "Source";
    private const string DbscScheme = DbscDefaults.AuthenticationScheme;
    private const string RefreshScheme = DbscScheme + ".Refresh";
    private const string SessionScheme = DbscScheme + ".Session";

    [Fact]
    public void PostConfigure_PreservesExistingTicketDataFormat_ForRefreshScheme()
    {
        var sut = CreateDerivedPostConfigure(refreshScheme: RefreshScheme);
        var sentinel = CreateSentinelFormat();
        var options = new CookieAuthenticationOptions { TicketDataFormat = sentinel };
        // The DBSC extension assigns the cookie name (and refresh path) before post-configure runs.
        options.Cookie.Name = ".AspNetCore." + RefreshScheme;

        sut.PostConfigure(RefreshScheme, options);

        // Protection is left untouched...
        Assert.Same(sentinel, options.TicketDataFormat);
        // ...but the post-configure still ran (copied source lifetime, applied refresh path scope, kept the name).
        Assert.Equal(TimeSpan.FromHours(3), options.ExpireTimeSpan);
        Assert.Equal("/.well-known/dbsc", options.Cookie.Build(new DefaultHttpContext()).Path);
        Assert.Equal(".AspNetCore." + RefreshScheme, options.Cookie.Name);
    }

    [Fact]
    public void PostConfigure_PreservesExistingTicketDataFormat_ForSessionScheme()
    {
        var sut = CreateDerivedPostConfigure(sessionScheme: SessionScheme);
        var sentinel = CreateSentinelFormat();
        var options = new CookieAuthenticationOptions { TicketDataFormat = sentinel };
        options.Cookie.Name = ".AspNetCore." + SessionScheme;

        sut.PostConfigure(SessionScheme, options);

        Assert.Same(sentinel, options.TicketDataFormat);
        Assert.Equal(TimeSpan.FromHours(3), options.ExpireTimeSpan);
        Assert.Equal(".AspNetCore." + SessionScheme, options.Cookie.Name);
    }

    [Fact]
    public void DerivedCookieSchemes_RemainDataProtected_AndSchemeKeyed()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
            .AddCookie(SourceScheme)
            .AddDbsc(DbscScheme, options => options.SourceScheme = SourceScheme);
        using var provider = services.BuildServiceProvider();

        var monitor = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        var source = monitor.Get(SourceScheme);
        var refresh = monitor.Get(RefreshScheme);
        var session = monitor.Get(SessionScheme);

        // All three schemes end up with a data-protecting ticket format.
        Assert.NotNull(source.TicketDataFormat);
        Assert.NotNull(refresh.TicketDataFormat);
        Assert.NotNull(session.TicketDataFormat);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("test")), RefreshScheme);
        var protectedByRefresh = refresh.TicketDataFormat.Protect(ticket);

        // The refresh format round-trips its own payload (real protection, not a no-op).
        Assert.True(CanUnprotect(refresh.TicketDataFormat, protectedByRefresh));

        // Protection is scheme-keyed: neither the session nor the source format can read the
        // refresh scheme's payload, proving each derived scheme keeps its own protector and the
        // source scheme's protection is independent.
        Assert.False(CanUnprotect(session.TicketDataFormat, protectedByRefresh));
        Assert.False(CanUnprotect(source.TicketDataFormat, protectedByRefresh));
    }

    [Fact]
    public void PostConfigure_RefreshScheme_InheritsSlidingExpiration_FromSource()
    {
        var sut = CreateDerivedPostConfigure(refreshScheme: RefreshScheme, sourceSlidingExpiration: true);
        var options = new CookieAuthenticationOptions();
        options.Cookie.Name = ".AspNetCore." + RefreshScheme;

        sut.PostConfigure(RefreshScheme, options);

        // The refresh cookie ages like the auth cookie it replaces: sliding inherited, lifetime copied.
        Assert.True(options.SlidingExpiration);
        Assert.Equal(TimeSpan.FromHours(3), options.ExpireTimeSpan);
    }

    [Fact]
    public void PostConfigure_RefreshScheme_RespectsDisabledSlidingExpiration_FromSource()
    {
        var sut = CreateDerivedPostConfigure(refreshScheme: RefreshScheme, sourceSlidingExpiration: false);
        var options = new CookieAuthenticationOptions();
        options.Cookie.Name = ".AspNetCore." + RefreshScheme;

        sut.PostConfigure(RefreshScheme, options);

        // When the source app opts out of sliding, the refresh cookie matches it.
        Assert.False(options.SlidingExpiration);
    }

    [Fact]
    public void PostConfigure_SessionScheme_DisablesSlidingExpiration_EvenWhenSourceSlides()
    {
        var sut = CreateDerivedPostConfigure(sessionScheme: SessionScheme, sourceSlidingExpiration: true);
        var options = new CookieAuthenticationOptions();
        options.Cookie.Name = ".AspNetCore." + SessionScheme;

        sut.PostConfigure(SessionScheme, options);

        // The short-lived session cookie is deliberately non-sliding regardless of the source.
        Assert.False(options.SlidingExpiration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(".example.com")]
    public void PostConfigure_RefreshScheme_InheritsCookieAttributes_FromSource(string? sourceDomain)
    {
        var sut = CreateDerivedPostConfigure(refreshScheme: RefreshScheme, sourceDomain: sourceDomain);
        var options = new CookieAuthenticationOptions();
        options.Cookie.Name = ".AspNetCore." + RefreshScheme;

        sut.PostConfigure(RefreshScheme, options);

        Assert.Equal(SameSiteMode.None, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(sourceDomain, options.Cookie.Domain);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(".example.com")]
    public void PostConfigure_SessionScheme_InheritsCookieAttributes_FromSource(string? sourceDomain)
    {
        var sut = CreateDerivedPostConfigure(sessionScheme: SessionScheme, sourceDomain: sourceDomain);
        var options = new CookieAuthenticationOptions();
        options.Cookie.Name = ".AspNetCore." + SessionScheme;

        sut.PostConfigure(SessionScheme, options);

        Assert.Equal(SameSiteMode.None, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(sourceDomain, options.Cookie.Domain);
    }

    private static PostConfigureDbscDerivedCookieOptions CreateDerivedPostConfigure(
        string? refreshScheme = null,
        string? sessionScheme = null,
        bool sourceSlidingExpiration = true,
        string? sourceDomain = null)
    {
        var sourceSchemes = new DbscSourceSchemes();
        if (refreshScheme is not null)
        {
            sourceSchemes.RefreshSchemes[refreshScheme] = DbscScheme;
        }
        if (sessionScheme is not null)
        {
            sourceSchemes.SessionSchemes[sessionScheme] = DbscScheme;
        }

        var dbscOptions = new Mock<IOptionsMonitor<DbscOptions>>();
        dbscOptions
            .Setup(monitor => monitor.Get(DbscScheme))
            .Returns(new DbscOptions { SourceScheme = SourceScheme });

        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<CookieAuthenticationOptions>(SourceScheme, o =>
        {
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.None;
            o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            o.Cookie.Domain = sourceDomain;
            o.ExpireTimeSpan = TimeSpan.FromHours(3);
            o.SlidingExpiration = sourceSlidingExpiration;
        });

        return new PostConfigureDbscDerivedCookieOptions(
            Options.Create(sourceSchemes),
            dbscOptions.Object,
            services.BuildServiceProvider());
    }

    private static TicketDataFormat CreateSentinelFormat()
        => new(new EphemeralDataProtectionProvider().CreateProtector("sentinel"));

    private static bool CanUnprotect(ISecureDataFormat<AuthenticationTicket> format, string value)
    {
        try
        {
            return format.Unprotect(value) is not null;
        }
        catch
        {
            return false;
        }
    }
}
