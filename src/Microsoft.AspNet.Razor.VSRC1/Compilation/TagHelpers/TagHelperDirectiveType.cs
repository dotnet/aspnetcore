// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Compilation.TagHelpers
{
    /// <summary>
    /// The type of tag helper directive.
    /// </summary>
    public enum TagHelperDirectiveType
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
}