// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents all the options you can use to configure the cookies middleware used by the identity system.
/// </summary>
public class IdentityConstants
{
    private const string CookiePrefix = "Identity";

    /// <summary>
    /// The scheme used to identify application authentication cookies.
    /// </summary>
    public static readonly string ApplicationScheme = CookiePrefix + ".Application";

    /// <summary>
    /// The scheme used to identify external authentication cookies.
    /// </summary>
    public static readonly string ExternalScheme = CookiePrefix + ".External";

    /// <summary>
    /// The scheme used to identify Two Factor authentication cookies for saving the Remember Me state.
    /// </summary>
    public static readonly string TwoFactorRememberMeScheme = CookiePrefix + ".TwoFactorRememberMe";

    /// <summary>
    /// The scheme used to identify Two Factor authentication cookies for round tripping user identities.
    /// </summary>
    public static readonly string TwoFactorUserIdScheme = CookiePrefix + ".TwoFactorUserId";
}
