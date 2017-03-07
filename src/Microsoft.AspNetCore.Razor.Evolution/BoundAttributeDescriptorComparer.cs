// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class BoundAttributeDescriptorComparer : IEqualityComparer<BoundAttributeDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="BoundAttributeDescriptorComparer"/>.
        /// </summary>
        public static readonly BoundAttributeDescriptorComparer Default = new BoundAttributeDescriptorComparer();

        /// <summary>
        /// A default instance of the <see cref="BoundAttributeDescriptorComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly BoundAttributeDescriptorComparer CaseSensitive =
            new BoundAttributeDescriptorComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;

        private BoundAttributeDescriptorComparer(bool caseSensitive = false)
        {
            if (caseSensitive)
            {
                _stringComparer = StringComparer.Ordinal;
                _stringComparison = StringComparison.Ordinal;
            }
            else
            {
                _stringComparer = StringComparer.OrdinalIgnoreCase;
                _stringComparison = StringComparison.OrdinalIgnoreCase;
            }
        }

        public virtual bool Equals(BoundAttributeDescriptor descriptorX, BoundAttributeDescriptor descriptorY)
        {
            if (object.ReferenceEquals(descriptorX, descriptorY))
            {
                return true;
            }

            if (descriptorX == null ^ descriptorY == null)
            {
                return false;
            }

            return descriptorX != null &&
                string.Equals(descriptorX.Kind, descriptorY.Kind, StringComparison.Ordinal) &&
                descriptorX.IsIndexerStringProperty == descriptorY.IsIndexerStringProperty &&
                descriptorX.IsEnum == descriptorY.IsEnum &&
                string.Equals(descriptorX.Name, descriptorY.Name, _stringComparison) &&
                string.Equals(descriptorX.IndexerNamePrefix, descriptorY.IndexerNamePrefix, _stringComparison) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.IndexerTypeName, descriptorY.IndexerTypeName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Documentation, descriptorY.Documentation, StringComparison.Ordinal) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(descriptorX.Diagnostics, descriptorY.Diagnostics) &&
                Enumerable.SequenceEqual(
                    descriptorX.Metadata.OrderBy(propertyX => propertyX.Key, StringComparer.Ordinal),
                    descriptorY.Metadata.OrderBy(propertyY => propertyY.Key, StringComparer.Ordinal));
        }

        public virtual int GetHashCode(BoundAttributeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(descriptor.Kind);
            hashCodeCombiner.Add(descriptor.IsIndexerStringProperty);
            hashCodeCombiner.Add(descriptor.IsEnum);
            hashCodeCombiner.Add(descriptor.Name, _stringComparer);
            hashCodeCombiner.Add(descriptor.IndexerNamePrefix, _stringComparer);
            hashCodeCombiner.Add(descriptor.TypeName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.IndexerTypeName, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.Documentation, StringComparer.Ordinal);
            hashCodeCombiner.Add(descriptor.DisplayName, StringComparer.Ordinal);

            return hashCodeCombiner.CombinedHash;
        }
    }
}