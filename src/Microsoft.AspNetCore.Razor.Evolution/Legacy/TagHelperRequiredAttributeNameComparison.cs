// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// Acceptable <see cref="TagHelperRequiredAttributeDescriptor.Name"/> comparison modes.
    /// </summary>
    internal enum TagHelperRequiredAttributeNameComparison
    {
        /// <summary>
        /// HTML attribute name case insensitively matches <see cref="TagHelperRequiredAttributeDescriptor.Name"/>.
        /// </summary>
        FullMatch,

        /// <summary>
        /// HTML attribute name case insensitively starts with <see cref="TagHelperRequiredAttributeDescriptor.Name"/>.
        /// </summary>
        PrefixMatch,
    }
}
