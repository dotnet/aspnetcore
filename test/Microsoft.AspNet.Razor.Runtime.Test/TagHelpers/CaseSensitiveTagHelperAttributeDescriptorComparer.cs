// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CaseSensitiveTagHelperAttributeDescriptorComparer : TagHelperAttributeDescriptorComparer
    {
        public new static readonly CaseSensitiveTagHelperAttributeDescriptorComparer Default =
            new CaseSensitiveTagHelperAttributeDescriptorComparer();

        private CaseSensitiveTagHelperAttributeDescriptorComparer()
            : base()
        {
        }

        public override bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
        {
            if (descriptorX == descriptorY)
            {
                return true;
            }

            // Normal comparer doesn't care about case, in tests we do. Also double-check IsStringProperty though
            // it is inferred from TypeName.
            return base.Equals(descriptorX, descriptorY) &&
                descriptorX.IsStringProperty == descriptorY.IsStringProperty &&
                string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal);
        }

        public override int GetHashCode(TagHelperAttributeDescriptor descriptor)
        {
            // Rarely if ever hash TagHelperAttributeDescriptor. If we do, ignore IsStringProperty since it should
            // not vary for a given TypeName i.e. will not change the bucket.
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode(descriptor))
                .Add(descriptor.Name, StringComparer.Ordinal)
                .CombinedHash;
        }
    }
}