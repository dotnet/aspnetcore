// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.UI;

/// <summary>
/// Records a recent confirmation of an existing credential, bound to the user and security stamp.
/// Requires a store that supports security stamps so that setting a password invalidates the marker.
/// Verification returns false for unsupported stores, and callers must check
/// <see cref="UserManager{TUser}.SupportsUserSecurityStamp"/> before marking a confirmation.
/// </summary>
internal static class ReauthenticationMarker
{
    private const string CookieName = "Identity.Reauthentication";
    private const string ProtectorPurpose = "Microsoft.AspNetCore.Identity.UI.ReauthenticationMarker.v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    /// <exception cref="NotSupportedException">The user store does not support security stamps.</exception>
    public static async Task MarkAsync<TUser>(HttpContext context, UserManager<TUser> userManager, TUser user)
        where TUser : class
    {
        if (!userManager.SupportsUserSecurityStamp)
        {
            throw new NotSupportedException("Reauthentication requires a user store that supports security stamps.");
        }

        var payload = await GetExpectedPayloadAsync(userManager, user);
        var protectedPayload = GetProtector(context).Protect(payload, Lifetime);
        context.Response.Cookies.Append(CookieName, protectedPayload, GetCookieOptions(context));
    }

    public static async Task<bool> IsVerifiedAsync<TUser>(HttpContext context, UserManager<TUser> userManager, TUser user)
        where TUser : class
    {
        if (!userManager.SupportsUserSecurityStamp ||
            !context.Request.Cookies.TryGetValue(CookieName, out var protectedPayload) ||
            string.IsNullOrEmpty(protectedPayload))
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
            // The marker expired, was tampered with, or was protected with a retired key.
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
        => context.RequestServices.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();

    private static CookieOptions GetCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        // Matches the identity cookies, which use CookieSecurePolicy.SameAsRequest so that the
        // UI still works over plain HTTP in development.
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
    };
}
