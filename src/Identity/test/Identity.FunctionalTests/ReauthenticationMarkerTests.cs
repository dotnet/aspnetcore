// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Identity.UI;

public class ReauthenticationMarkerTests
{
    private const string CookieName = "Identity.UI.Reauthentication";
    private const string ProtectorPurpose = "Microsoft.AspNetCore.Identity.UI.ReauthenticationMarker.v1";

    [Fact]
    public async Task MarkedUserIsVerifiedForFiveMinutes()
    {
        var now = DateTimeOffset.UtcNow;
        using var services = CreateServices();
        var user = new IdentityUser { Id = "user", SecurityStamp = "stamp" };
        var userManager = CreateUserManager();
        var markContext = CreateContext(services);

        await ReauthenticationMarker.MarkAsync(markContext, userManager.Object, user);

        var cookie = GetMarkerCookie(markContext);
        var protector = services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();
        var payload = protector.Unprotect(cookie.Value.Value, out var expiration);
        var verificationContext = CreateContext(services, cookie.Value);

        Assert.Equal("user:stamp", payload);
        Assert.InRange(expiration, now.AddMinutes(4).AddSeconds(59), now.AddMinutes(5).AddSeconds(1));
        Assert.True(await ReauthenticationMarker.IsVerifiedAsync(verificationContext, userManager.Object, user));
    }

    [Fact]
    public async Task TamperedMarkerIsRejected()
    {
        using var services = CreateServices();
        var user = new IdentityUser { Id = "user", SecurityStamp = "stamp" };
        var userManager = CreateUserManager();
        var markContext = CreateContext(services);
        await ReauthenticationMarker.MarkAsync(markContext, userManager.Object, user);
        var cookie = GetMarkerCookie(markContext);
        var verificationContext = CreateContext(services, $"{cookie.Value}tampered");

        Assert.False(await ReauthenticationMarker.IsVerifiedAsync(verificationContext, userManager.Object, user));
    }

    [Fact]
    public async Task MarkerForDifferentUserOrSecurityStampIsRejected()
    {
        using var services = CreateServices();
        var user = new IdentityUser { Id = "user", SecurityStamp = "stamp" };
        var userManager = CreateUserManager();
        var markContext = CreateContext(services);
        await ReauthenticationMarker.MarkAsync(markContext, userManager.Object, user);
        var cookie = GetMarkerCookie(markContext);

        var differentUserContext = CreateContext(services, cookie.Value);
        var staleStampContext = CreateContext(services, cookie.Value);
        var differentUser = new IdentityUser { Id = "different-user", SecurityStamp = "stamp" };
        user.SecurityStamp = "updated-stamp";

        Assert.False(await ReauthenticationMarker.IsVerifiedAsync(differentUserContext, userManager.Object, differentUser));
        Assert.False(await ReauthenticationMarker.IsVerifiedAsync(staleStampContext, userManager.Object, user));
    }

    [Fact]
    public async Task ExpiredMarkerIsRejected()
    {
        using var services = CreateServices();
        var user = new IdentityUser { Id = "user", SecurityStamp = "stamp" };
        var userManager = CreateUserManager();
        var protector = services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();
        var expiredPayload = protector.Protect("user:stamp", DateTimeOffset.UtcNow.AddMinutes(-1));
        var context = CreateContext(services, expiredPayload);

        Assert.False(await ReauthenticationMarker.IsVerifiedAsync(context, userManager.Object, user));
    }

    private static ServiceProvider CreateServices() =>
        new ServiceCollection()
            .AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>()
            .BuildServiceProvider();

    private static DefaultHttpContext CreateContext(IServiceProvider services, StringSegment? cookieValue = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services,
        };
        if (cookieValue is not null)
        {
            context.Request.Headers.Cookie = $"{CookieName}={cookieValue}";
        }

        return context;
    }

    private static Mock<UserManager<IdentityUser>> CreateUserManager()
    {
        var userStore = new Mock<IUserStore<IdentityUser>>();
        var userManager = new Mock<UserManager<IdentityUser>>(
            userStore.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        userManager
            .Setup(manager => manager.GetUserIdAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync((IdentityUser user) => user.Id);
        userManager
            .Setup(manager => manager.GetSecurityStampAsync(It.IsAny<IdentityUser>()))
            .ReturnsAsync((IdentityUser user) => user.SecurityStamp);
        return userManager;
    }

    private static SetCookieHeaderValue GetMarkerCookie(HttpContext context)
    {
        var cookie = SetCookieHeaderValue.Parse(context.Response.Headers.SetCookie.ToString());
        Assert.Equal(CookieName, cookie.Name.Value);
        return cookie;
    }
}
