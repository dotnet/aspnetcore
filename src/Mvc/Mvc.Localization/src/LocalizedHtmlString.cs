// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Localization;

/// <summary>
/// An <see cref="IHtmlContent"/> with localized content.
/// </summary>
public class LocalizedHtmlString : IHtmlContent
{
    private readonly object[] _arguments;

    /// <summary>
    /// Creates an instance of <see cref="LocalizedHtmlString"/>.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="value">The string resource.</param>
    public LocalizedHtmlString(string name, string value)
        : this(name, value, isResourceNotFound: false, arguments: Array.Empty<object>())
    {
    }

    /// <summary>
    /// Creates an instance of <see cref="LocalizedHtmlString"/>.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="value">The string resource.</param>
    /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
    public LocalizedHtmlString(string name, string value, bool isResourceNotFound)
        : this(name, value, isResourceNotFound, arguments: Array.Empty<object>())
    {
    }

    /// <summary>
    /// Creates an instance of <see cref="LocalizedHtmlString"/>.
    /// </summary>
    /// <param name="name">The name of the string resource.</param>
    /// <param name="value">The string resource.</param>
    /// <param name="isResourceNotFound">A flag that indicates if the resource is not found.</param>
    /// <param name="arguments">The values to format the <paramref name="value"/> with.</param>
    public LocalizedHtmlString(string name, string value, bool isResourceNotFound, params object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(arguments);

        Name = name;
        Value = value;
        IsResourceNotFound = isResourceNotFound;
        _arguments = arguments;
    }

    /// <summary>
    /// The name of the string resource.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The original resource string, prior to formatting with any constructor arguments.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets a flag that indicates if the resource is not found.
    /// </summary>
    public bool IsResourceNotFound { get; }

    /// <inheritdoc />
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(encoder);

        var formattableString = new HtmlFormattableString(Value, _arguments);
        formattableString.WriteTo(writer, encoder);
    }
}
