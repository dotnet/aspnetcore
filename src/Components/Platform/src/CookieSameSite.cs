// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Specifies the SameSite attribute of a cookie.
/// </summary>
/// <remarks>
/// A value that this type does not define can be created from its string, such as one that a newer browser supports.
/// A browser that does not support the value rejects it.
/// </remarks>
[JsonConverter(typeof(WebIdlEnumJsonConverter<CookieSameSite>))]
public readonly record struct CookieSameSite : IWebIdlEnum<CookieSameSite>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CookieSameSite"/> with the specified string.
    /// </summary>
    /// <param name="value">The string that the browser uses for the value, such as <c>"lax"</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public CookieSameSite(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>
    /// Gets a value that makes the browser send the cookie only with same-site requests.
    /// </summary>
    public static CookieSameSite Strict { get; } = new("strict");

    /// <summary>
    /// Gets a value that makes the browser send the cookie with same-site requests and with cross-site top-level navigations.
    /// </summary>
    public static CookieSameSite Lax { get; } = new("lax");

    /// <summary>
    /// Gets a value that makes the browser send the cookie with both same-site and cross-site requests.
    /// </summary>
    public static CookieSameSite None { get; } = new("none");

    /// <summary>
    /// Gets the string that the browser uses for the value.
    /// </summary>
    public string Value { get; }

    static CookieSameSite IWebIdlEnum<CookieSameSite>.Create(string value) => new(value);

    /// <summary>
    /// Returns the string that the browser uses for the value.
    /// </summary>
    /// <returns>The string that the browser uses for the value.</returns>
    public override string ToString() => Value ?? string.Empty;
}
