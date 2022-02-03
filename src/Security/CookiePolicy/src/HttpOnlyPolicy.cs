// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.CookiePolicy;

/// <summary>
/// Describes the HttpOnly behavior for cookies.
/// </summary>
public enum HttpOnlyPolicy
{
    /// <summary>
    /// The cookie does not have a configured HttpOnly behavior. This cookie can be accessed by
    /// JavaScript <c>document.cookie</c> API.
    /// </summary>
    None,

    /// <summary>
    /// The cookie is configured with a HttpOnly attribute. This cookie inaccessible to the
    /// JavaScript <c>document.cookie</c> API.
    /// </summary>
    Always
}
