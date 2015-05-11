// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Internal;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperAttributeDescriptor}"/> used to check equality between
    /// two <see cref="TagHelperAttributeDescriptor"/>s.
    /// </summary>
    public class TagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagHelperAttributeDescriptorComparer"/>.
        /// </summary>
        public static readonly TagHelperAttributeDescriptorComparer Default =
            new TagHelperAttributeDescriptorComparer();

        /// <summary>
        /// Initializes a new <see cref="TagHelperAttributeDescriptorComparer"/> instance.
        /// </summary>
        protected TagHelperAttributeDescriptorComparer()
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// Determines equality based on <see cref="TagHelperAttributeDescriptor.Name"/>,
        /// <see cref="TagHelperAttributeDescriptor.PropertyName"/>,
        /// and <see cref="TagHelperAttributeDescriptor.TypeName"/>.
        /// </remarks>
        public virtual bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public virtual int GetHashCode([NotNull] TagHelperAttributeDescriptor descriptor)
        {
            return HashCodeCombiner.Start()
                .Add(descriptor.Name, StringComparer.OrdinalIgnoreCase)
                .Add(descriptor.PropertyName, StringComparer.Ordinal)
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}