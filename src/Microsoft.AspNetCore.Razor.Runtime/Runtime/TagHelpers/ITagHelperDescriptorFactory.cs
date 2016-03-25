// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/> instances.
    /// </summary>
    public interface ITagHelperDescriptorFactory
    {
        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="assemblyName">The assembly name that contains <paramref name="type"/>.</param>
        /// <param name="type">The <see cref="Type"/> to create a <see cref="TagHelperDescriptor"/> from.
        /// </param>
        /// <param name="errorSink">The <see cref="ErrorSink"/> used to collect <see cref="RazorError"/>s encountered
        /// when creating <see cref="TagHelperDescriptor"/>s for the given <paramref name="type"/>.</param>
        /// <returns>
        /// A collection of <see cref="TagHelperDescriptor"/>s that describe the given <paramref name="type"/>.
        /// </returns>
        IEnumerable<TagHelperDescriptor> CreateDescriptors(string assemblyName, Type type, ErrorSink errorSink);
    }
}
