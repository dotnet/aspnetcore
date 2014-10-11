// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CompleteTagHelperDescriptorComparer : TagHelperDescriptorComparer, IEqualityComparer<TagHelperDescriptor>
    {
        public new static readonly CompleteTagHelperDescriptorComparer Default =
            new CompleteTagHelperDescriptorComparer();

        private CompleteTagHelperDescriptorComparer()
        {
        }

        bool IEqualityComparer<TagHelperDescriptor>.Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            return base.Equals(descriptorX, descriptorY) &&
                   descriptorX.Attributes.SequenceEqual(descriptorY.Attributes,
                                                        CompleteTagHelperAttributeDescriptorComparer.Default);
        }

        int IEqualityComparer<TagHelperDescriptor>.GetHashCode(TagHelperDescriptor descriptor)
        {
            return HashCodeCombiner.Start()
                                   .Add(base.GetHashCode())
                                   .Add(descriptor.Attributes)
                                   .CombinedHash;
        }

        private class CompleteTagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
        {
            public static readonly CompleteTagHelperAttributeDescriptorComparer Default =
                new CompleteTagHelperAttributeDescriptorComparer();

            private CompleteTagHelperAttributeDescriptorComparer()
            {
            }

            public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
            {
                return descriptorX.AttributeName == descriptorY.AttributeName &&
                       descriptorX.AttributePropertyName == descriptorY.AttributePropertyName;
            }

            public int GetHashCode(TagHelperAttributeDescriptor descriptor)
            {
                return HashCodeCombiner.Start()
                                       .Add(descriptor.AttributeName)
                                       .Add(descriptor.AttributePropertyName)
                                       .CombinedHash;
            }
        }
    }
}