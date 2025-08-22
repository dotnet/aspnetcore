// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Provides correct handling for FragmentString value when needed to generate a URI string
/// </summary>
[DebuggerDisplay("{Value}")]
public readonly struct FragmentString : IEquatable<FragmentString>
{
    /// <summary>
    /// Represents the empty fragment string. This field is read-only.
    /// </summary>
    public static readonly FragmentString Empty = new FragmentString(string.Empty);

    private readonly string _value;

    /// <summary>
    /// Initialize the fragment string with a given value. This value must be in escaped and delimited format with
    /// a leading '#' character.
    /// </summary>
    /// <param name="value">The fragment string to be assigned to the Value property.</param>
    public FragmentString(string value)
    {
        if (!string.IsNullOrEmpty(value) && value[0] != '#')
        {
            throw new ArgumentException("The leading '#' must be included for a non-empty fragment.", nameof(value));
        }
        _value = value;
    }

    /// <summary>
    /// The escaped fragment string with the leading '#' character
    /// </summary>
    public string Value
    {
        get { return _value; }
    }

    /// <summary>
    /// True if the fragment string is not empty
    /// </summary>
    public bool HasValue
    {
        get { return !string.IsNullOrEmpty(_value); }
    }

    /// <summary>
    /// Provides the fragment string escaped in a way which is correct for combining into the URI representation.
    /// A leading '#' character will be included unless the Value is null or empty. Characters which are potentially
    /// dangerous are escaped.
    /// </summary>
    /// <returns>The fragment string value</returns>
    public override string ToString()
    {
        return ToUriComponent();
    }

    /// <summary>
    /// Provides the fragment string escaped in a way which is correct for combining into the URI representation.
    /// A leading '#' character will be included unless the Value is null or empty. Characters which are potentially
    /// dangerous are escaped.
    /// </summary>
    /// <returns>The fragment string value</returns>
    public string ToUriComponent()
    {
        // Escape things properly so System.Uri doesn't mis-interpret the data.
        return HasValue ? _value : string.Empty;
    }

    /// <summary>
    /// Returns an FragmentString given the fragment as it is escaped in the URI format. The string MUST NOT contain any
    /// value that is not a fragment.
    /// </summary>
    /// <param name="uriComponent">The escaped fragment as it appears in the URI format.</param>
    /// <returns>The resulting FragmentString</returns>
    public static FragmentString FromUriComponent(string uriComponent)
    {
        if (String.IsNullOrEmpty(uriComponent))
        {
            return Empty;
        }
        return new FragmentString(uriComponent);
    }

    /// <summary>
    /// Returns an FragmentString given the fragment as from a Uri object. Relative Uri objects are not supported.
    /// </summary>
    /// <param name="uri">The Uri object</param>
    /// <returns>The resulting FragmentString</returns>
    public static FragmentString FromUriComponent(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        string fragmentValue = uri.GetComponents(UriComponents.Fragment, UriFormat.UriEscaped);
        if (!string.IsNullOrEmpty(fragmentValue))
        {
            fragmentValue = "#" + fragmentValue;
        }
        return new FragmentString(fragmentValue);
    }

    /// <summary>
    /// Evaluates if the current fragment is equal to another fragment <paramref name="other"/>.
    /// </summary>
    /// <param name="other">A <see cref="FragmentString" /> to compare.</param>
    /// <returns><see langword="true" /> if the fragments are equal.</returns>
    public bool Equals(FragmentString other)
    {
        if (!HasValue && !other.HasValue)
        {
            return true;
        }
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Evaluates if the current fragment is equal to an object <paramref name="obj"/>.
    /// </summary>
    /// <param name="obj">An object to compare.</param>
    /// <returns><see langword="true" /> if the fragments are equal.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return !HasValue;
        }
        return obj is FragmentString value && Equals(value);
    }

    /// <summary>
    /// Gets a hash code for the value.
    /// </summary>
    /// <returns>The hash code as an <see cref="int"/>.</returns>
    public override int GetHashCode()
    {
        return (HasValue ? _value.GetHashCode() : 0);
    }

    /// <summary>
    /// Evaluates if one fragment is equal to another.
    /// </summary>
    /// <param name="left">A <see cref="FragmentString"/> instance.</param>
    /// <param name="right">A <see cref="FragmentString"/> instance.</param>
    /// <returns><see langword="true" /> if the fragments are equal.</returns>
    public static bool operator ==(FragmentString left, FragmentString right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Evalutes if one fragment is not equal to another.
    /// </summary>
    /// <param name="left">A <see cref="FragmentString"/> instance.</param>
    /// <param name="right">A <see cref="FragmentString"/> instance.</param>
    /// <returns><see langword="true" /> if the fragments are not equal.</returns>
    public static bool operator !=(FragmentString left, FragmentString right)
    {
        return !left.Equals(right);
    }
}
