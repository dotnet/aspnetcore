// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Defines how a tag helper will utilize its inner HTML.
    /// </summary>
    public enum ContentBehavior
    {
        /// <summary>
        /// Indicates that a tag helper will not modify its content in any way. This is the default
        /// <see cref="ContentBehavior"/>.
        /// </summary>
        /// <remarks>Children of the current tag helper will execute after the current tag helper.</remarks>
        None,

        /// <summary>
        /// Indicates the tag helper's content should be appended to what its children generate.
        /// </summary>
        /// <remarks>Children of the current tag helper will execute before the current tag helper.</remarks>
        Append,

        /// <summary>
        /// Indicates that the tag helper will modify its HTML content. Therefore this <see cref="ContentBehavior"/>
        /// enables the tag helper to examine the content its children generate.
        /// </summary>
        /// <remarks>Children of the current tag helper will execute before the current tag helper.</remarks>
        Modify,

        /// <summary>
        /// Indicates the tag helper's content should be prepended to what its children generate.
        /// </summary>
        /// <remarks>Children of the current tag helper will execute after the current tag helper.</remarks>
        Prepend,

        /// <summary>
        /// Indicates the tag helper's content should replace the HTML its children generate.
        /// </summary>
        /// <remarks>Children of the current tag helper will not execute.</remarks>
        Replace,
    }
}