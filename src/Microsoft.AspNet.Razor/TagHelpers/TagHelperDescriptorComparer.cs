// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

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
        /// <see cref="TagHelperDescriptor.RequiredAttributes"/>, <see cref="TagHelperDescriptor.AllowedChildren"/>,
        /// and <see cref="TagHelperDescriptor.TagStructure"/>.
        /// Ignores <see cref="TagHelperDescriptor.DesignTimeDescriptor"/> because it can be inferred directly from
        /// <see cref="TagHelperDescriptor.TypeName"/> and <see cref="TagHelperDescriptor.AssemblyName"/>.
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
                    StringComparer.OrdinalIgnoreCase) &&
                (descriptorX.AllowedChildren == descriptorY.AllowedChildren ||
                (descriptorX.AllowedChildren != null &&
                descriptorY.AllowedChildren != null &&
                Enumerable.SequenceEqual(
                    descriptorX.AllowedChildren.OrderBy(child => child, StringComparer.OrdinalIgnoreCase),
                    descriptorY.AllowedChildren.OrderBy(child => child, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase))) &&
                descriptorX.TagStructure == descriptorY.TagStructure;
        }

        /// <inheritdoc />
        public virtual int GetHashCode(TagHelperDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hashCodeCombiner = HashCodeCombiner.Start()
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .Add(descriptor.TagName, StringComparer.OrdinalIgnoreCase)
                .Add(descriptor.AssemblyName, StringComparer.Ordinal)
                .Add(descriptor.TagStructure);

            var attributes = descriptor.RequiredAttributes.OrderBy(
                attribute => attribute,
                StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in attributes)
            {
                hashCodeCombiner.Add(attribute, StringComparer.OrdinalIgnoreCase);
            }

            if (descriptor.AllowedChildren != null)
            {
                var allowedChildren = descriptor.AllowedChildren.OrderBy(child => child, StringComparer.OrdinalIgnoreCase);
                foreach (var child in allowedChildren)
                {
                    hashCodeCombiner.Add(child, StringComparer.OrdinalIgnoreCase);
                }
            }

            return hashCodeCombiner.CombinedHash;
        }
    }
}