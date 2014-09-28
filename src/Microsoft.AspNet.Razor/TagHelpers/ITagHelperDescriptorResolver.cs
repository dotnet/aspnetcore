// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Contract used to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public interface ITagHelperDescriptorResolver
    {
        /// <summary>
        /// Resolves <see cref="TagHelperDescriptor"/>s matching the given <paramref name="lookupText"/>.
        /// </summary>
        /// <param name="lookupText">
        /// A <see cref="string"/> used to find tag helper <see cref="Type"/>s.
        /// </param>
        /// <returns>An <see cref="IEnumerable{TagHelperDescriptor}"/> of <see cref="TagHelperDescriptor"/>s matching 
        /// the given <paramref name="lookupText"/>.</returns>
        IEnumerable<TagHelperDescriptor> Resolve(string lookupText);
    }
}