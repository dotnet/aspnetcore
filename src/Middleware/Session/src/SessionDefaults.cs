// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Session;

/// <summary>
/// Represents defaults for the Session.
/// </summary>
public static class SessionDefaults
{
    /// <summary>
    /// Represent the default cookie name, which is ".AspNetCore.Session".
    /// </summary>
    public static readonly string CookieName = ".AspNetCore.Session";

    /// <summary>
    /// Represents the default path used to create the cookie, which is "/".
    /// </summary>
    public static readonly string CookiePath = "/";
}
