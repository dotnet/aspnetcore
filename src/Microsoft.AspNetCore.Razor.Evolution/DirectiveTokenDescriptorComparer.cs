// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DirectiveTokenDescriptorComparer : IEqualityComparer<DirectiveTokenDescriptor>
    {
        public static readonly DirectiveTokenDescriptorComparer Default = new DirectiveTokenDescriptorComparer();

        protected DirectiveTokenDescriptorComparer()
        {
        }

        public bool Equals(DirectiveTokenDescriptor descriptorX, DirectiveTokenDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            return descriptorX != null &&
                descriptorX.Kind == descriptorY.Kind;
        }

        public int GetHashCode(DirectiveTokenDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor.Kind.GetHashCode();
        }
    }
}
