// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Specifies the SameSite attribute of a cookie.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CookieSameSite>))]
public enum CookieSameSite
{
    /// <summary>
    /// The browser sends the cookie only with same-site requests.
    /// </summary>
    [JsonStringEnumMemberName("strict")]
    Strict,

    /// <summary>
    /// The browser sends the cookie with same-site requests and with cross-site top-level navigations.
    /// </summary>
    [JsonStringEnumMemberName("lax")]
    Lax,

    /// <summary>
    /// The browser sends the cookie with both same-site and cross-site requests.
    /// </summary>
    [JsonStringEnumMemberName("none")]
    None,
}
