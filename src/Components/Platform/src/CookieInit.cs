// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Represents the name, value, and attributes of a cookie to set.
/// </summary>
public sealed class CookieInit
{
    /// <summary>
    /// Gets or sets the name of the cookie.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the cookie.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets the expiration time of the cookie in milliseconds since the Unix epoch,
    /// or <see langword="null"/> for a cookie without an expiration time.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Expires { get; set; }

    /// <summary>
    /// Gets or sets the domain of the cookie, or <see langword="null"/> to restrict the cookie to the host of the current page.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the path of the cookie. The default is <c>"/"</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Path { get; set; } = "/";

    /// <summary>
    /// Gets or sets the SameSite attribute of the cookie. The default is <see cref="CookieSameSite.Strict"/>.
    /// </summary>
    public CookieSameSite SameSite { get; set; } = CookieSameSite.Strict;

    // Chromium crashes the page on maxAge: null, although null means the same as a missing member.
    /// <summary>
    /// Gets or sets the maximum age of the cookie in seconds, or <see langword="null"/> for a cookie without a maximum age.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? MaxAge { get; set; }
}
