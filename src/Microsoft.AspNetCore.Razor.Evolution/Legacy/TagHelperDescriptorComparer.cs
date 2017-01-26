// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperDescriptor}"/> used to check equality between
    /// two <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    internal class TagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagHelperDescriptorComparer"/>.
        /// </summary>
        public static readonly TagHelperDescriptorComparer Default = new TagHelperDescriptorComparer();

        /// <summary>
        /// An instance of <see cref="TagHelperDescriptorComparer"/> that only compares 
        /// <see cref="TagHelperDescriptor.TypeName"/>.
        /// </summary>
        public static readonly TagHelperDescriptorComparer TypeName = new TypeNameTagHelperDescriptorComparer();

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
                string.Equals(
                    descriptorX.RequiredParent,
                    descriptorY.RequiredParent,
                    StringComparison.OrdinalIgnoreCase) &&
                Enumerable.SequenceEqual(
                    descriptorX.RequiredAttributes.OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase),
                    descriptorY.RequiredAttributes.OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase),
                    TagHelperRequiredAttributeDescriptorComparer.Default) &&
                (descriptorX.AllowedChildren == descriptorY.AllowedChildren ||
                (descriptorX.AllowedChildren != null &&
                descriptorY.AllowedChildren != null &&
                Enumerable.SequenceEqual(
                    descriptorX.AllowedChildren.OrderBy(child => child, StringComparer.OrdinalIgnoreCase),
                    descriptorY.AllowedChildren.OrderBy(child => child, StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase))) &&
                descriptorX.TagStructure == descriptorY.TagStructure &&
                Enumerable.SequenceEqual(
                    descriptorX.PropertyBag.OrderBy(propertyX => propertyX.Key, StringComparer.Ordinal),
                    descriptorY.PropertyBag.OrderBy(propertyY => propertyY.Key, StringComparer.Ordinal));
        }

        /// <inheritdoc />
        public virtual int GetHashCode(TagHelperDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(descriptor.TypeName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.TagName, StringComparer.OrdinalIgnoreCase);
            hashCodeCombiner.Add(descriptor.AssemblyName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.RequiredParent, StringComparer.OrdinalIgnoreCase);
            hashCodeCombiner.Add(descriptor.TagStructure);

            var attributes = descriptor.RequiredAttributes.OrderBy(
                attribute => attribute.Name,
                StringComparer.OrdinalIgnoreCase);
            foreach (var attribute in attributes)
            {
                hashCodeCombiner.Add(TagHelperRequiredAttributeDescriptorComparer.Default.GetHashCode(attribute));
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

        private class TypeNameTagHelperDescriptorComparer : TagHelperDescriptorComparer
        {
            public override bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
            {
                if (object.ReferenceEquals(descriptorX, descriptorY))
                {
                    return true;
                }
                else if (descriptorX == null ^ descriptorY == null)
                {
                    return false;
                }
                else
                {
                    return string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
                }
            }

            public override int GetHashCode(TagHelperDescriptor descriptor)
            {
                if (descriptor == null)
                {
                    throw new ArgumentNullException(nameof(descriptor));
                }

                return descriptor.TypeName == null ? 0 : StringComparer.Ordinal.GetHashCode(descriptor.TypeName);
            }
        }
    }
}