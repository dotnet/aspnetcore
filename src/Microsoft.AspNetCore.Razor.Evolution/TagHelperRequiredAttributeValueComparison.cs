// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// Acceptable <see cref="TagHelperRequiredAttributeDescriptor.Value"/> comparison modes.
    /// </summary>
    public enum TagHelperRequiredAttributeValueComparison
    {
        /// <summary>
        /// HTML attribute value always matches <see cref="TagHelperRequiredAttributeDescriptor.Value"/>.
        /// </summary>
        None,

        /// <summary>
        /// HTML attribute value case sensitively matches <see cref="TagHelperRequiredAttributeDescriptor.Value"/>.
        /// </summary>
        FullMatch,

        /// <summary>
        /// HTML attribute value case sensitively starts with <see cref="TagHelperRequiredAttributeDescriptor.Value"/>.
        /// </summary>
        PrefixMatch,

        /// <summary>
        /// HTML attribute value case sensitively ends with <see cref="TagHelperRequiredAttributeDescriptor.Value"/>.
        /// </summary>
        SuffixMatch,
    }
}
