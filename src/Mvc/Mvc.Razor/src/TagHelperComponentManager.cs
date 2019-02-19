// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
