using System.Security.Cryptography;
using BlazorWebCSharp._1.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace BlazorWebCSharp._1.Components.Account;

// Records that the user recently confirmed their identity with a credential the account already
// has. Creating a new login credential requires this marker.
//
// The marker is a data-protected payload of the user id and the current security stamp, so
// changing the password or signing out everywhere invalidates any marker already issued. It is
// valid for five minutes rather than for a single use.
internal static class PasskeyReauthentication
{
    private const string CookieName = "Identity.Reauthentication";
    private const string ProtectorPurpose = "BlazorWebCSharp._1.Components.Account.PasskeyReauthentication.v1";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public static async Task MarkAsync(HttpContext context, UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var payload = await GetExpectedPayloadAsync(userManager, user);
        var protectedPayload = GetProtector(context).Protect(payload, Lifetime);
        context.Response.Cookies.Append(CookieName, protectedPayload, GetCookieOptions(context));
    }

    public static async Task<bool> IsVerifiedAsync(HttpContext context, UserManager<ApplicationUser> userManager, ApplicationUser user)
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
            // The marker expired, was tampered with, or was protected with a retired key.
            return false;
        }

        var expectedPayload = await GetExpectedPayloadAsync(userManager, user);
        return string.Equals(payload, expectedPayload, StringComparison.Ordinal);
    }

    public static void Clear(HttpContext context)
        => context.Response.Cookies.Delete(CookieName, GetCookieOptions(context));

    private static async Task<string> GetExpectedPayloadAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
        => $"{await userManager.GetUserIdAsync(user)}:{await userManager.GetSecurityStampAsync(user)}";

    private static ITimeLimitedDataProtector GetProtector(HttpContext context)
        => context.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProtectorPurpose)
            .ToTimeLimitedDataProtector();

    private static CookieOptions GetCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        // Matches the identity cookies, which use CookieSecurePolicy.SameAsRequest so that the
        // template still works over plain HTTP in development.
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
    };
}
