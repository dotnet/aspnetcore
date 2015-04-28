// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Internal.Web.Utils;

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
            return
                attributeX == attributeY ||
                // Normal comparer doesn't care about the Name case, in tests we do.
                string.Equals(attributeX.Name, attributeY.Name, StringComparison.Ordinal) &&
                Equals(attributeX.Value, attributeY.Value);
        }

        public int GetHashCode(IReadOnlyTagHelperAttribute attribute)
        {
            return HashCodeCombiner
                .Start()
                .Add(attribute.Name, StringComparer.Ordinal)
                .Add(attribute.Value)
                .CombinedHash;
        }
    }
}