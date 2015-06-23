// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// A metadata class containing design time information about a tag helper.
    /// </summary>
    public class TagHelperDesignTimeDescriptor
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperDesignTimeDescriptor"/>.
        /// </summary>
        /// <param name="summary">A summary on how to use a tag helper.</param>
        /// <param name="remarks">Remarks on how to use a tag helper.</param>
        /// <param name="outputElementHint">The HTML element a tag helper may output.</param>
        public TagHelperDesignTimeDescriptor(string summary, string remarks, string outputElementHint)
        {
            Summary = summary;
            Remarks = remarks;
            OutputElementHint = outputElementHint;
        }

        /// <summary>
        /// A summary of how to use a tag helper.
        /// </summary>
        public string Summary { get; }

        /// <summary>
        /// Remarks about how to use a tag helper.
        /// </summary>
        public string Remarks { get; }

        /// <summary>
        /// The HTML element a tag helper may output.
        /// </summary>
        /// <remarks>
        /// In IDEs supporting IntelliSense, may override the HTML information provided at design time.
        /// </remarks>
        public string OutputElementHint { get; }
    }
}