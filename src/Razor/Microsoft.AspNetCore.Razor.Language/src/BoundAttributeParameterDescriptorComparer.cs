// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal class BoundAttributeParameterDescriptorComparer : IEqualityComparer<BoundAttributeParameterDescriptor>
{
    /// <summary>
    /// A default instance of the <see cref="BoundAttributeParameterDescriptorComparer"/>.
    /// </summary>
    public static readonly BoundAttributeParameterDescriptorComparer Default = new BoundAttributeParameterDescriptorComparer();

    private BoundAttributeParameterDescriptorComparer()
    {
    }

    public virtual bool Equals(BoundAttributeParameterDescriptor descriptorX, BoundAttributeParameterDescriptor descriptorY)
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
            descriptorX.IsEnum == descriptorY.IsEnum &&
            string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
            string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
            string.Equals(descriptorX.Documentation, descriptorY.Documentation, StringComparison.Ordinal) &&
            string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal) &&
            Enumerable.SequenceEqual(
                descriptorX.Metadata.OrderBy(propertyX => propertyX.Key, StringComparer.Ordinal),
                descriptorY.Metadata.OrderBy(propertyY => propertyY.Key, StringComparer.Ordinal));
    }

    public virtual int GetHashCode(BoundAttributeParameterDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var hash = HashCodeCombiner.Start();
        hash.Add(descriptor.Kind, StringComparer.Ordinal);
        hash.Add(descriptor.Name, StringComparer.Ordinal);
        hash.Add(descriptor.TypeName, StringComparer.Ordinal);
        hash.Add(descriptor.Metadata?.Count);

        return hash.CombinedHash;
    }
}
