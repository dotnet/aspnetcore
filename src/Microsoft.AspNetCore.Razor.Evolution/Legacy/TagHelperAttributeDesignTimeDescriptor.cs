// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// A metadata class containing information about tag helper use.
    /// </summary>
    internal class TagHelperAttributeDesignTimeDescriptor
    {
        /// <summary>
        /// A summary of how to use a tag helper.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Remarks about how to use a tag helper.
        /// </summary>
        public string Remarks { get; set; }
    }
}