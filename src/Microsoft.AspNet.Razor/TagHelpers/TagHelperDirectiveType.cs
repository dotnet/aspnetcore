// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// The type of tag helper directive.
    /// </summary>
    public enum TagHelperDirectiveType
    {
        /// <summary>
        /// An @addtaghelper directive.
        /// </summary>
        AddTagHelper,

        /// <summary>
        /// A @removetaghelper directive.
        /// </summary>
        RemoveTagHelper
    }
}