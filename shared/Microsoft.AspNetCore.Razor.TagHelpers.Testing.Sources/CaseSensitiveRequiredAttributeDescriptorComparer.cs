// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.Extensions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers.Testing
{
    internal class CaseSensitiveTagHelperRequiredAttributeDescriptorComparer : TagHelperRequiredAttributeDescriptorComparer
    {
        public new static readonly CaseSensitiveTagHelperRequiredAttributeDescriptorComparer Default =
            new CaseSensitiveTagHelperRequiredAttributeDescriptorComparer();

        private CaseSensitiveTagHelperRequiredAttributeDescriptorComparer()
            : base()
        {
        }

        public override bool Equals(TagHelperRequiredAttributeDescriptor descriptorX, TagHelperRequiredAttributeDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            Assert.True(base.Equals(descriptorX, descriptorY));
            Assert.Equal(descriptorX.Name, descriptorY.Name, StringComparer.Ordinal);

            return true;
        }

        public override int GetHashCode(TagHelperRequiredAttributeDescriptor descriptor)
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(base.GetHashCode(descriptor));
            hashCodeCombiner.Add(descriptor.Name, StringComparer.Ordinal);

            return hashCodeCombiner.CombinedHash;
        }
    }
}