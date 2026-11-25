// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Specifies whether a lock can be held by more than one holder at a time.
/// </summary>
/// <remarks>
/// A value that this type does not define can be created from its string, such as one that a newer browser returns.
/// </remarks>
[JsonConverter(typeof(WebIdlEnumJsonConverter<LockMode>))]
public readonly struct LockMode : IEquatable<LockMode>, IWebIdlEnum<LockMode>
{
    // Null only in default(LockMode); the constructor never stores null.
    private readonly string? _value;

    /// <summary>
    /// Initializes a new instance of <see cref="LockMode"/> with the specified string.
    /// </summary>
    /// <param name="value">The string that the browser uses for the value, such as <c>"exclusive"</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public LockMode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    /// <summary>
    /// Gets a value that indicates that the lock can be held by several holders at the same time.
    /// </summary>
    public static LockMode Shared { get; } = new("shared");

    /// <summary>
    /// Gets a value that indicates that the lock can be held by only one holder at a time.
    /// </summary>
    public static LockMode Exclusive { get; } = new("exclusive");

    /// <summary>
    /// Gets the string that the browser uses for the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">The value is <see langword="default"/> and has no string.</exception>
    public string Value => _value ?? throw new InvalidOperationException("The value is uninitialized.");

    /// <summary>
    /// Indicates whether this value has the same string as another value. The comparison is ordinal.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns><see langword="true"/> if both values have the same string; otherwise, <see langword="false"/>.</returns>
    public bool Equals(LockMode other) => _value == other._value;

    /// <summary>
    /// Indicates whether this value has the same string as the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="LockMode"/> with the same string; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is LockMode other && Equals(other);

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
    public static bool operator ==(LockMode left, LockMode right) => left.Equals(right);

    /// <summary>
    /// Indicates whether two values have different strings.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><see langword="true"/> if the values have different strings; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(LockMode left, LockMode right) => !left.Equals(right);

    static LockMode IWebIdlEnum<LockMode>.Create(string value) => new(value);

    /// <summary>
    /// Returns the string that the browser uses for the value, or an empty string if the value is <see langword="default"/>.
    /// </summary>
    /// <returns>The string that the browser uses for the value, or an empty string.</returns>
    public override string ToString() => _value ?? string.Empty;
}
