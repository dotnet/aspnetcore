// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A metadata class containing information about tag helper use.
    /// </summary>
    public class TagHelperAttributeDesignTimeDescriptor
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperDesignTimeDescriptor"/>.
        /// </summary>
        /// <param name="summary">A summary on how to use a tag helper.</param>
        /// <param name="remarks">Remarks on how to use a tag helper.</param>
        public TagHelperAttributeDesignTimeDescriptor(string summary, string remarks)
        {
            Summary = summary;
            Remarks = remarks;
        }

        /// <summary>
        /// A summary of how to use a tag helper.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// Remarks about how to use a tag helper.
        /// </summary>
        public string Remarks { get; }
    }
}