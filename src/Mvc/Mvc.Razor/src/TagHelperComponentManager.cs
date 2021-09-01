// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// The default implementation of the <see cref="ITagHelperComponentManager"/>.
    /// </summary>
    internal class TagHelperComponentManager : ITagHelperComponentManager
    {
        /// <summary>
        /// Creates a new <see cref="TagHelperComponentManager"/>.
        /// </summary>
        /// <param name="tagHelperComponents">The collection of <see cref="ITagHelperComponent"/>s.</param>
        public TagHelperComponentManager(IEnumerable<ITagHelperComponent> tagHelperComponents)
        {
            if (tagHelperComponents == null)
            {
                throw new ArgumentNullException(nameof(tagHelperComponents));
            }

            Components = new List<ITagHelperComponent>(tagHelperComponents);
        }

        /// <inheritdoc />
        public ICollection<ITagHelperComponent> Components { get; }
    }
}
