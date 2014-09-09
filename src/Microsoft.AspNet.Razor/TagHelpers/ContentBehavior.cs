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
        /// Indicates that the tag helper will not modify its inner HTML in any way. This is the default
        /// <see cref="ContentBehavior"/>.
        /// </summary>
        /// <remarks>Children of the current tag helper will execute after the current tag helper.</remarks>
        None,

        /// <summary>
        /// Indicates that the tag helper wants anything within its tag builder's inner HTML to be
        /// appended to content its children generate.
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
        /// Indicates that the tag helper wants anything within its tag builder's inner HTML to be
        /// prepended to the content its children generate.
        /// </summary>
        /// <remarks>Children of the current tag helper will execute after the current tag helper.</remarks>
        Prepend,

        /// <summary>
        /// Indicates that the tag helper wants anything within its tag builder's inner HTML to
        /// replace any HTML inside of it.
        /// </summary>
        /// <remarks>Children of the current tag helper will not execute.</remarks>
        Replace,
    }
}