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
public readonly struct CookieSameSite : IEquatable<CookieSameSite>, IWebIdlEnum<CookieSameSite>
{
    private const string DefaultValue = "strict";

    // The default value is stored as null so that default(CookieSameSite) has the same field as Strict.
    private readonly string? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="CookieSameSite"/> with the specified string.
    /// </summary>
    /// <param name="value">The string that the browser uses for the value, such as <c>"lax"</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public CookieSameSite(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value == DefaultValue ? null : value;
    }

    /// <summary>
    /// Gets a value that makes the browser send the cookie only with same-site requests.
    /// </summary>
    public static CookieSameSite Strict { get; } = new(DefaultValue);

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
    public string Value => _value ?? DefaultValue;

    /// <summary>
    /// Indicates whether this value has the same string as another value. The comparison is ordinal.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> if both values have the same string; otherwise, <see langword="false"/>.</returns>
    public bool Equals(CookieSameSite other) => _value == other._value;

    /// <summary>
    /// Indicates whether this value has the same string as the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="CookieSameSite"/> with the same string; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is CookieSameSite other && Equals(other);

    /// <summary>
    /// Returns the hash code of the string.
    /// </summary>
    /// <returns>The hash code of the string.</returns>
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;

    /// <summary>
    /// Indicates whether two values have the same string.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if both values have the same string; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(CookieSameSite left, CookieSameSite right) => left.Equals(right);

    /// <summary>
    /// Indicates whether two values have different strings.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if the values have different strings; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(CookieSameSite left, CookieSameSite right) => !left.Equals(right);

    static CookieSameSite IWebIdlEnum<CookieSameSite>.Create(string value) => new(value);

    /// <summary>
    /// Returns the string that the browser uses for the value.
    /// </summary>
    /// <returns>The string that the browser uses for the value.</returns>
    public override string ToString() => Value;
}
