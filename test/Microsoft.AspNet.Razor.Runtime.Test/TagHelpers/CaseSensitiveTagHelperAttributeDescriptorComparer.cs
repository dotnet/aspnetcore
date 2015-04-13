// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CaseSensitiveTagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
    {
        public static readonly CaseSensitiveTagHelperAttributeDescriptorComparer Default =
            new CaseSensitiveTagHelperAttributeDescriptorComparer();

        private CaseSensitiveTagHelperAttributeDescriptorComparer()
        {
        }

        public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
        {
            return
                // Normal comparer doesn't care about case, in tests we do.
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
                string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
        }

        public int GetHashCode(TagHelperAttributeDescriptor descriptor)
        {
            return HashCodeCombiner
                .Start()
                .Add(descriptor.Name, StringComparer.Ordinal)
                .Add(descriptor.PropertyName, StringComparer.Ordinal)
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}