// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperDescriptor}"/> used to check equality between
    /// two <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagHelperDescriptorComparer"/>.
        /// </summary>
        public static readonly TagHelperDescriptorComparer Default = new TagHelperDescriptorComparer();

        /// <summary>
        /// Initializes a new <see cref="TagHelperDescriptorComparer"/> instance.
        /// </summary>
        protected TagHelperDescriptorComparer()
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// Determines equality based on <see cref="TagHelperDescriptor.TypeName"/>,
        /// <see cref="TagHelperDescriptor.AssemblyName"/>, <see cref="TagHelperDescriptor.TagName"/>,
        /// and <see cref="TagHelperDescriptor.RequiredAttributes"/>.
        /// </remarks>
        public virtual bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TagName, descriptorY.TagName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(descriptorX.AssemblyName, descriptorY.AssemblyName, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(
                    descriptorX.RequiredAttributes.OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase),
                    descriptorY.RequiredAttributes.OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public virtual int GetHashCode([NotNull] TagHelperDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start()
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .Add(descriptor.TagName, StringComparer.OrdinalIgnoreCase)
                .Add(descriptor.AssemblyName, StringComparer.Ordinal);

            var attributes = descriptor.RequiredAttributes.OrderBy(
                attribute => attribute,
                StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in attributes)
            {
                hashCodeCombiner.Add(attribute, StringComparer.OrdinalIgnoreCase);
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}