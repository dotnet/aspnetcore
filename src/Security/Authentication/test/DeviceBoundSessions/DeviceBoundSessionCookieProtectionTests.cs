// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionCookieProtectionTests
{
    private const string SourceScheme = "Source";
    private const string RefreshScheme = "Source.Dbsc.Refresh";
    private const string SessionScheme = "Source.Dbsc.Session";

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
        Assert.Equal("/.well-known/dbsc", options.Cookie.Path);
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
            .AddDeviceBoundSession(SourceScheme);
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

    private static PostConfigureDeviceBoundSessionDerivedCookieOptions CreateDerivedPostConfigure(
        string? refreshScheme = null,
        string? sessionScheme = null)
    {
        var sourceSchemes = new DeviceBoundSessionSourceSchemes();
        if (refreshScheme is not null)
        {
            sourceSchemes.RefreshSchemes[refreshScheme] = SourceScheme;
        }
        if (sessionScheme is not null)
        {
            sourceSchemes.SessionSchemes[sessionScheme] = SourceScheme;
        }

        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<CookieAuthenticationOptions>(SourceScheme, o =>
        {
            o.Cookie.HttpOnly = true;
            o.ExpireTimeSpan = TimeSpan.FromHours(3);
        });

        return new PostConfigureDeviceBoundSessionDerivedCookieOptions(
            Options.Create(sourceSchemes),
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
