// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// A metadata class containing design time information about a tag helper.
    /// </summary>
    internal class TagHelperDesignTimeDescriptor
    {
        /// <summary>
        /// A summary of how to use a tag helper.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Remarks about how to use a tag helper.
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// The HTML element a tag helper may output.
        /// </summary>
        /// <remarks>
        /// In IDEs supporting IntelliSense, may override the HTML information provided at design time.
        /// </remarks>
        public string OutputElementHint { get; set; }
    }
}