// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Contains information related to the execution of <see cref="ITagHelper"/>s.
    /// </summary>
    public class TagHelperContext
    {
        /// <summary>
        /// Instantiates a new <see cref="TagHelperContext"/>.
        /// </summary>
        /// <param name="allAttributes">Every attribute associated with the current HTML element.</param>
        public TagHelperContext([NotNull] IDictionary<string, object> allAttributes)
        {
            AllAttributes = allAttributes;
        }

        /// <summary>
        /// Every attribute associated with the current HTML element.
        /// </summary>
        public IDictionary<string, object> AllAttributes { get; private set; }
    }
}