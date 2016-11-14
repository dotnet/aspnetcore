// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
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
}