// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DirectiveDescriptorComparer : IEqualityComparer<DirectiveDescriptor>
    {
        public static readonly DirectiveDescriptorComparer Default = new DirectiveDescriptorComparer();

        protected DirectiveDescriptorComparer()
        {
        }

        public bool Equals(DirectiveDescriptor descriptorX, DirectiveDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                string.Equals(descriptorX.Directive, descriptorY.Directive, StringComparison.Ordinal) &&
                descriptorX.Kind == descriptorY.Kind &&
                Enumerable.SequenceEqual(
                    descriptorX.Tokens,
                    descriptorY.Tokens,
                    DirectiveTokenDescriptorComparer.Default);
        }

        public int GetHashCode(DirectiveDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var hash = HashCodeCombiner.Start();
            hash.Add(descriptor.Directive, StringComparer.Ordinal);
            hash.Add(descriptor.Kind);

            if (descriptor.Tokens != null)
            {
                for (var i = 0; i < descriptor.Tokens.Count; i++)
                {
                    hash.Add(descriptor.Tokens[i]);
                }
            }

            return hash.CombinedHash;
        }
    }
}
