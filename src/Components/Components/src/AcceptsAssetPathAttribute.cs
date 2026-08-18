// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Declares an HTML element and attribute combination that accepts static asset-path expansion.
/// </summary>
/// <remarks>
/// The Razor compiler discovers this metadata on public classes named <c>AssetPathAttributes</c>.
/// </remarks>
/// <example>
/// <code>
/// [AcceptsAssetPath("img", "src")]
/// public static class AssetPathAttributes
/// {
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AcceptsAssetPathAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="AcceptsAssetPathAttribute"/>.
    /// </summary>
    /// <param name="elementName">The HTML element name.</param>
    /// <param name="attributeName">The HTML attribute name.</param>
    public AcceptsAssetPathAttribute(string elementName, string attributeName)
    {
        ArgumentNullException.ThrowIfNull(elementName);
        ArgumentNullException.ThrowIfNull(attributeName);

        ElementName = elementName;
        AttributeName = attributeName;
    }

    /// <summary>
    /// Gets the HTML element name.
    /// </summary>
    public string ElementName { get; }

    /// <summary>
    /// Gets the HTML attribute name.
    /// </summary>
    public string AttributeName { get; }
}
