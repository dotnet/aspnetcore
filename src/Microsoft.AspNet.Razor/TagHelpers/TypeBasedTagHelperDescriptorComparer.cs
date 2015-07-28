// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperDescriptor}"/> that checks equality between two
    /// <see cref="TagHelperDescriptor"/>s using only their <see cref="TagHelperDescriptor.AssemblyName"/>s and
    /// <see cref="TagHelperDescriptor.TypeName"/>s.
    /// </summary>
    /// <remarks>
    /// This class is intended for scenarios where Reflection-based information is all important i.e.
    /// <see cref="TagHelperDescriptor.RequiredAttributes"/>, <see cref="TagHelperDescriptor.TagName"/>, and related
    /// properties are not relevant.
    /// </remarks>
    public class TypeBasedTagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TypeBasedTagHelperDescriptorComparer"/>.
        /// </summary>
        public static readonly TypeBasedTagHelperDescriptorComparer Default =
            new TypeBasedTagHelperDescriptorComparer();

        private TypeBasedTagHelperDescriptorComparer()
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// Determines equality based on <see cref="TagHelperDescriptor.AssemblyName"/> and
        /// <see cref="TagHelperDescriptor.TypeName"/>.
        /// </remarks>
        public bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                string.Equals(descriptorX.AssemblyName, descriptorY.AssemblyName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public int GetHashCode([NotNull] TagHelperDescriptor descriptor)
        {
            return HashCodeCombiner.Start()
                .Add(descriptor.AssemblyName, StringComparer.Ordinal)
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}
