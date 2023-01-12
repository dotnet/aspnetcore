// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.Localization;

/// <summary>
/// A locale specific string.
/// </summary>
public class LocalizedString
{
    /// <summary>
    /// Creates a new <see cref="LocalizedString"/>.
    /// </summary>
    /// <param name="name">The name of the string in the resource it was loaded from.</param>
    /// <param name="value">The actual string.</param>
    public LocalizedString(string name, string value)
        : this(name, value, resourceNotFound: false)
    {
    }

    /// <summary>
    /// Creates a new <see cref="LocalizedString"/>.
    /// </summary>
    /// <param name="name">The name of the string in the resource it was loaded from.</param>
    /// <param name="value">The actual string.</param>
    /// <param name="resourceNotFound">Whether the string was not found in a resource. Set this to <c>true</c> to indicate an alternate string value was used.</param>
    public LocalizedString(string name, string value, bool resourceNotFound)
        : this(name, value, resourceNotFound, searchedLocation: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="LocalizedString"/>.
    /// </summary>
    /// <param name="name">The name of the string in the resource it was loaded from.</param>
    /// <param name="value">The actual string.</param>
    /// <param name="resourceNotFound">Whether the string was not found in a resource. Set this to <c>true</c> to indicate an alternate string value was used.</param>
    /// <param name="searchedLocation">The location which was searched for a localization value.</param>
    public LocalizedString(string name, string value, bool resourceNotFound, string? searchedLocation)
    {
        ArgumentNullThrowHelper.ThrowIfNull(name);
        ArgumentNullThrowHelper.ThrowIfNull(value);

        Name = name;
        Value = value;
        ResourceNotFound = resourceNotFound;
        SearchedLocation = searchedLocation;
    }

    /// <summary>
    /// Implicitly converts the <see cref="LocalizedString"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="localizedString">The string to be implicitly converted.</param>
    [return: NotNullIfNotNull("localizedString")]
    public static implicit operator string?(LocalizedString localizedString)
    {
        return localizedString?.Value;
    }

    /// <summary>
    /// The name of the string in the resource it was loaded from.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The actual string.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Whether the string was not found in a resource. If <c>true</c>, an alternate string value was used.
    /// </summary>
    public bool ResourceNotFound { get; }

    /// <summary>
    /// The location which was searched for a localization value.
    /// </summary>
    public string? SearchedLocation { get; }

    /// <summary>
    /// Returns the actual string.
    /// </summary>
    /// <returns>The actual string.</returns>
    public override string ToString() => Value;
}
