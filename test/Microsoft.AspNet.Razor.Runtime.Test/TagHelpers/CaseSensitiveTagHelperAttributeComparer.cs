// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CaseSensitiveTagHelperAttributeComparer : IEqualityComparer<IReadOnlyTagHelperAttribute>
    {
        public readonly static CaseSensitiveTagHelperAttributeComparer Default =
            new CaseSensitiveTagHelperAttributeComparer();

        private CaseSensitiveTagHelperAttributeComparer()
        {
        }

        public bool Equals(IReadOnlyTagHelperAttribute attributeX, IReadOnlyTagHelperAttribute attributeY)
        {
            if (attributeX == attributeY)
            {
                return true;
            }

            // Normal comparer (TagHelperAttribute.Equals()) doesn't care about the Name case, in tests we do.
            return attributeX != null &&
                string.Equals(attributeX.Name, attributeY.Name, StringComparison.Ordinal) &&
                attributeX.Minimized == attributeY.Minimized &&
                (attributeX.Minimized || Equals(attributeX.Value, attributeY.Value));
        }

        public int GetHashCode(IReadOnlyTagHelperAttribute attribute)
        {
            return attribute.GetHashCode();
        }
    }
}