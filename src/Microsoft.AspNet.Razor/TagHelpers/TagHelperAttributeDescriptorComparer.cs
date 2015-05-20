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
        /// Determines equality based on <see cref="TagHelperAttributeDescriptor.IsIndexer"/>,
        /// <see cref="TagHelperAttributeDescriptor.Name"/>, <see cref="TagHelperAttributeDescriptor.PropertyName"/>,
        /// and <see cref="TagHelperAttributeDescriptor.TypeName"/>. Ignores
        /// <see cref="TagHelperAttributeDescriptor.IsStringProperty"/> because it can be inferred directly from
        /// <see cref="TagHelperAttributeDescriptor.TypeName"/>.
        /// </remarks>
        public virtual bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            // Check Name and TypeName though each property in a particular tag helper has at most two
            // TagHelperAttributeDescriptors (one for the indexer and one not). May be comparing attributes between
            // tag helpers and should be as specific as we can.
            return descriptorX != null &&
                descriptorX.IsIndexer == descriptorY.IsIndexer &&
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public virtual int GetHashCode([NotNull] TagHelperAttributeDescriptor descriptor)
        {
            // Rarely if ever hash TagHelperAttributeDescriptor. If we do, include the Name and TypeName since context
            // information is not available in the hash.
            return HashCodeCombiner.Start()
                .Add(descriptor.IsIndexer)
                .Add(descriptor.Name, StringComparer.OrdinalIgnoreCase)
                .Add(descriptor.PropertyName, StringComparer.Ordinal)
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}