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
        /// <param name="uniqueId">The unique identifier for the source element this <see cref="TagHelperContext" /> applies to.</param>
        public TagHelperContext([NotNull] IDictionary<string, object> allAttributes, [NotNull] string uniqueId)
        {
            AllAttributes = allAttributes;
            UniqueId = uniqueId;
        }

        /// <summary>
        /// Every attribute associated with the current HTML element.
        /// </summary>
        public IDictionary<string, object> AllAttributes { get; private set; }

        /// <summary>
        /// An identifier unique to the HTML element this context is for.
        /// </summary>
        public string UniqueId { get; private set; }
    }
}