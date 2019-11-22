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

        /// <summary>
        /// A default instance of the <see cref="RequiredAttributeDescriptorComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly RequiredAttributeDescriptorComparer CaseSensitive =
            new RequiredAttributeDescriptorComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;

        private RequiredAttributeDescriptorComparer(bool caseSensitive = false)
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
                descriptorX.NameComparison == descriptorY.NameComparison &&
                descriptorX.ValueComparison == descriptorY.ValueComparison &&
                string.Equals(descriptorX.Name, descriptorY.Name, _stringComparison) &&
                string.Equals(descriptorX.Value, descriptorY.Value, StringComparison.Ordinal) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public virtual int GetHashCode(RequiredAttributeDescriptor descriptor)
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(descriptor.Name, _stringComparer);

            return hash.CombinedHash;
        }
    }
}
