// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class AllowedChildTagDescriptorComparer : IEqualityComparer<AllowedChildTagDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="AllowedChildTagDescriptorComparer"/>.
        /// </summary>
        public static readonly AllowedChildTagDescriptorComparer Default =
            new AllowedChildTagDescriptorComparer();

        /// <summary>
        /// A default instance of the <see cref="AllowedChildTagDescriptorComparer"/> that does case-sensitive comparison.
        /// </summary>
        internal static readonly AllowedChildTagDescriptorComparer CaseSensitive =
            new AllowedChildTagDescriptorComparer(caseSensitive: true);

        private readonly StringComparer _stringComparer;
        private readonly StringComparison _stringComparison;

        private AllowedChildTagDescriptorComparer(bool caseSensitive = false)
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

            return descriptorX != null &&
                string.Equals(descriptorX.Name, descriptorY.Name, _stringComparison) &&
                string.Equals(descriptorX.DisplayName, descriptorY.DisplayName, StringComparison.Ordinal) &&
                Enumerable.SequenceEqual(descriptorX.Diagnostics, descriptorY.Diagnostics);
        }

        /// <inheritdoc />
        public virtual int GetHashCode(AllowedChildTagDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(descriptor.Name, _stringComparer);
            hashCodeCombiner.Add(descriptor.DisplayName, StringComparer.Ordinal);

            return hashCodeCombiner.CombinedHash;
        }
    }
}
