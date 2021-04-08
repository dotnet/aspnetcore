// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class BoundAttributeDescriptorComparer : IEqualityComparer<BoundAttributeDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="BoundAttributeDescriptorComparer"/>.
        /// </summary>
        public static readonly BoundAttributeDescriptorComparer Default = new BoundAttributeDescriptorComparer();

        private BoundAttributeDescriptorComparer()
        {
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

            return
                string.Equals(descriptorX.Kind, descriptorY.Kind, StringComparison.Ordinal) &&
                descriptorX.IsIndexerStringProperty == descriptorY.IsIndexerStringProperty &&
                descriptorX.IsEnum == descriptorY.IsEnum &&
                descriptorX.HasIndexer == descriptorY.HasIndexer &&
                descriptorX.CaseSensitive == descriptorY.CaseSensitive &&
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
                string.Equals(descriptorX.IndexerNamePrefix, descriptorY.IndexerNamePrefix, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.IndexerTypeName, descriptorY.IndexerTypeName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Documentation, descriptorY.Documentation, StringComparison.Ordinal) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal) &&
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

            var hash = HashCodeCombiner.Start();
            hash.Add(descriptor.Kind, StringComparer.Ordinal);
            hash.Add(descriptor.Name, StringComparer.Ordinal);

            if (descriptor.BoundAttributeParameters != null)
            {
                for (var i = 0; i < descriptor.BoundAttributeParameters.Count; i++)
                {
                    hash.Add(descriptor.BoundAttributeParameters[i]);
                }
            }

            foreach (var metadata in descriptor.Metadata)
            {
                hash.Add(metadata.Key);
                hash.Add(metadata.Value);
            }

            return hash.CombinedHash;
        }
    }
}
