// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// A mapping of a <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/> mode to its required attributes.
/// </summary>
/// <typeparam name="TMode">The type representing the <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s mode.</typeparam>
internal sealed class ModeAttributes<TMode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ModeAttributes{TMode}"/>.
    /// </summary>
    /// <param name="mode">The <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s mode.</param>
    /// <param name="attributes">The names of attributes required for this mode.</param>
    public ModeAttributes(TMode mode, string[] attributes)
    {
        Mode = mode;
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the <see cref="AspNetCore.Razor.TagHelpers.ITagHelper"/>'s mode.
    /// </summary>
    public TMode Mode { get; }

    /// <summary>
    /// Gets the names of attributes required for this mode.
    /// </summary>
    public string[] Attributes { get; }
}
