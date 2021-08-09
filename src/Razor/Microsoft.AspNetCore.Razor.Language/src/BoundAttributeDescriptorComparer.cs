// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal sealed class BoundAttributeDescriptorComparer : IEqualityComparer<BoundAttributeDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="BoundAttributeDescriptorComparer"/>.
        /// </summary>
        public static readonly BoundAttributeDescriptorComparer Default = new BoundAttributeDescriptorComparer();

        private BoundAttributeDescriptorComparer()
        {
        }

        public bool Equals(BoundAttributeDescriptor descriptorX, BoundAttributeDescriptor descriptorY)
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
                descriptorX.IsEditorRequired == descriptorY.IsEditorRequired &&
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

        public int GetHashCode(BoundAttributeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(descriptor.Kind, StringComparer.Ordinal);
            hash.Add(descriptor.Name, StringComparer.Ordinal);
            hash.Add(descriptor.IsEditorRequired);

            if (descriptor.BoundAttributeParameters != null)
            {
                for (var i = 0; i < descriptor.BoundAttributeParameters.Count; i++)
                {
                    hash.Add(descriptor.BoundAttributeParameters[i]);
                }
            }

            Debug.Assert(descriptor.MetadataImpl is not null, "We expect MetadataImpl to be configured in the default code path.");
            foreach (var metadata in descriptor.MetadataImpl)
            {
                hash.Add(metadata.Key);
                hash.Add(metadata.Value);
            }

            return hash.CombinedHash;
        }
    }
}
