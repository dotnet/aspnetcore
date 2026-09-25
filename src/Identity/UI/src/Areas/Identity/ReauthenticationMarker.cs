// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.UI;

internal static class ReauthenticationMarker
{
    private const string CookieName = "Identity.UI.Reauthentication";
    private const string ProtectorPurpose = "Microsoft.AspNetCore.Identity.UI.ReauthenticationMarker.v1";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public static async Task MarkAsync<TUser>(HttpContext context, UserManager<TUser> userManager, TUser user)
        where TUser : class
    {
        var payload = await GetExpectedPayloadAsync(userManager, user);
        var protectedPayload = GetProtector(context).Protect(payload, Lifetime);
        context.Response.Cookies.Append(CookieName, protectedPayload, GetCookieOptions(context));
    }

    public static async Task<bool> IsVerifiedAsync<TUser>(HttpContext context, UserManager<TUser> userManager, TUser user)
        where TUser : class
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var protectedPayload) || string.IsNullOrEmpty(protectedPayload))
        {
            return false;
        }

        string payload;
        try
        {
            payload = GetProtector(context).Unprotect(protectedPayload);
        }
        catch (CryptographicException)
        {
            return false;
        }

        var expectedPayload = await GetExpectedPayloadAsync(userManager, user);
        return string.Equals(payload, expectedPayload, StringComparison.Ordinal);
    }

    public static void Clear(HttpContext context)
        => context.Response.Cookies.Delete(CookieName, GetCookieOptions(context));

    private static async Task<string> GetExpectedPayloadAsync<TUser>(UserManager<TUser> userManager, TUser user)
        where TUser : class
        => $"{await userManager.GetUserIdAsync(user)}:{await userManager.GetSecurityStampAsync(user)}";

    private static ITimeLimitedDataProtector GetProtector(HttpContext context)
        => context.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();

    private static CookieOptions GetCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
    };
}
