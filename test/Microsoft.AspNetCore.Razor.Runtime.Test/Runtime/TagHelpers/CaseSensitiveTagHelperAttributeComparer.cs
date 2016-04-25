// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class CaseSensitiveTagHelperAttributeComparer : IEqualityComparer<TagHelperAttribute>
    {
        public readonly static CaseSensitiveTagHelperAttributeComparer Default =
            new CaseSensitiveTagHelperAttributeComparer();

        private CaseSensitiveTagHelperAttributeComparer()
        {
        }

        public bool Equals(TagHelperAttribute attributeX, TagHelperAttribute attributeY)
        {
            if (attributeX == attributeY)
            {
                return true;
            }

            // Normal comparer (TagHelperAttribute.Equals()) doesn't care about the Name case, in tests we do.
            return attributeX != null &&
                string.Equals(attributeX.Name, attributeY.Name, StringComparison.Ordinal) &&
                attributeX.ValueStyle == attributeY.ValueStyle &&
                (attributeX.ValueStyle == HtmlAttributeValueStyle.Minimized || Equals(attributeX.Value, attributeY.Value));
        }

        public int GetHashCode(TagHelperAttribute attribute)
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(attribute.Name, StringComparer.Ordinal);
            combiner.Add(attribute.GetHashCode());

            return combiner.CombinedHash;
        }
    }
}