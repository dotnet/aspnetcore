// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperRequiredAttributeDescriptor}"/> used to check equality between
    /// two <see cref="RequiredAttributeDescriptor"/>s.
    /// </summary>
    internal class RequiredAttributeDescriptorComparer : IEqualityComparer<RequiredAttributeDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="RequiredAttributeDescriptorComparer"/>.
        /// </summary>
        public static readonly RequiredAttributeDescriptorComparer Default =
            new RequiredAttributeDescriptorComparer();

        private RequiredAttributeDescriptorComparer()
        {
        }

        /// <inheritdoc />
        public virtual bool Equals(
            RequiredAttributeDescriptor descriptorX,
            RequiredAttributeDescriptor descriptorY)
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
                descriptorX.CaseSensitive == descriptorY.CaseSensitive &&
                descriptorX.NameComparison == descriptorY.NameComparison &&
                descriptorX.ValueComparison == descriptorY.ValueComparison &&
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
                string.Equals(descriptorX.Value, descriptorY.Value, StringComparison.Ordinal) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public virtual int GetHashCode(RequiredAttributeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(descriptor.Name, StringComparer.Ordinal);
            hash.Add(descriptor.Value, StringComparer.Ordinal);

            return hash.CombinedHash;
        }
    }
}
