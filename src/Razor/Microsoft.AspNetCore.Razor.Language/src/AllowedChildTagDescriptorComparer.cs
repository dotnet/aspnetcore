// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal class AllowedChildTagDescriptorComparer : IEqualityComparer<AllowedChildTagDescriptor>
{
    /// <summary>
    /// A default instance of the <see cref="AllowedChildTagDescriptorComparer"/>.
    /// </summary>
    public static readonly AllowedChildTagDescriptorComparer Default =
        new AllowedChildTagDescriptorComparer();

    private AllowedChildTagDescriptorComparer()
    {
    }

    /// <inheritdoc />
    public virtual bool Equals(
        AllowedChildTagDescriptor descriptorX,
        AllowedChildTagDescriptor descriptorY)
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
            string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
            string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public virtual int GetHashCode(AllowedChildTagDescriptor descriptor)
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(descriptor.Name, StringComparer.Ordinal);

        return hash.CombinedHash;
    }
}
