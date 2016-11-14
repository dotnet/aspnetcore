// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// Contract used to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    internal interface ITagHelperDescriptorResolver
    {
        /// <summary>
        /// Resolves <see cref="TagHelperDescriptor"/>s based on the given <paramref name="resolutionContext"/>.
        /// </summary>
        /// <param name="resolutionContext">
        /// <see cref="TagHelperDescriptorResolutionContext"/> used to resolve descriptors for the Razor page.
        /// </param>
        /// <returns>An <see cref="IEnumerable{TagHelperDescriptor}"/> of <see cref="TagHelperDescriptor"/>s based
        /// on the given <paramref name="resolutionContext"/>.</returns>
        IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext);
    }
}