// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// The structure the element should be written in.
/// </summary>
public enum TagStructure
{
    /// <summary>
    /// If no other tag helper applies to the same element and specifies a <see cref="TagStructure"/>,
    /// <see cref="NormalOrSelfClosing"/> will be used.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Element can be written as &lt;my-tag-helper&gt;&lt;/my-tag-helper&gt; or &lt;my-tag-helper /&gt;.
    /// </summary>
    NormalOrSelfClosing,

    /// <summary>
    /// Element can be written as &lt;my-tag-helper&gt; or &lt;my-tag-helper /&gt;.
    /// </summary>
    /// <remarks>Elements with a <see cref="WithoutEndTag"/> structure will never have any content.</remarks>
    WithoutEndTag
}
