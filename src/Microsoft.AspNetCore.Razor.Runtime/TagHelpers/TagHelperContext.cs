// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    /// <summary>
    /// Contains information related to the execution of <see cref="ITagHelper"/>s.
    /// </summary>
    public class TagHelperContext
    {
        private static ReadOnlyTagHelperAttributeList EmptyAttributes = new TagHelperAttributeList();

        /// <summary>
        /// Instantiates a new <see cref="TagHelperContext"/>.
        /// </summary>
        /// <param name="allAttributes">Every attribute associated with the current HTML element.</param>
        /// <param name="items">Collection of items used to communicate with other <see cref="ITagHelper"/>s.</param>
        /// <param name="uniqueId">The unique identifier for the source element this <see cref="TagHelperContext" />
        /// applies to.</param>
        public TagHelperContext(
            ReadOnlyTagHelperAttributeList allAttributes,
            IDictionary<object, object> items,
            string uniqueId)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (uniqueId == null)
            {
                throw new ArgumentNullException(nameof(uniqueId));
            }

            AllAttributes = allAttributes ?? EmptyAttributes;
            Items = items;
            UniqueId = uniqueId;
        }

        /// <summary>
        /// Every attribute associated with the current HTML element.
        /// </summary>
        public ReadOnlyTagHelperAttributeList AllAttributes { get; }

        /// <summary>
        /// Gets the collection of items used to communicate with other <see cref="ITagHelper"/>s.
        /// </summary>
        /// <remarks>
        /// This <see cref="IDictionary{object, object}"/> is copy-on-write in order to ensure items added to this
        /// collection are visible only to other <see cref="ITagHelper"/>s targeting child elements.
        /// </remarks>
        public IDictionary<object, object> Items { get; }

        /// <summary>
        /// An identifier unique to the HTML element this context is for.
        /// </summary>
        public string UniqueId { get; }
    }
}