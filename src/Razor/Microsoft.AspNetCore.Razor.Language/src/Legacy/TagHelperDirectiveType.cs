// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

/// <summary>
/// The type of tag helper directive.
/// </summary>
internal enum TagHelperDirectiveType
{
    /// <summary>
    /// An <c>@addTagHelper</c> directive.
    /// </summary>
    AddTagHelper,

    /// <summary>
    /// A <c>@removeTagHelper</c> directive.
    /// </summary>
    RemoveTagHelper,

    /// <summary>
    /// A <c>@tagHelperPrefix</c> directive.
    /// </summary>
    TagHelperPrefix
}
