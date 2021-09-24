// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class MemberExpressionCacheKeyComparer : IEqualityComparer<MemberExpressionCacheKey>
    {
        public static readonly MemberExpressionCacheKeyComparer Instance = new MemberExpressionCacheKeyComparer();

        public bool Equals(MemberExpressionCacheKey x, MemberExpressionCacheKey y)
        {
            if (x.ModelType != y.ModelType)
            {
                return false;
            }

            var xEnumerator = x.GetEnumerator();
            var yEnumerator = y.GetEnumerator();

            while (xEnumerator.MoveNext())
            {
                if (!yEnumerator.MoveNext())
                {
                    return false;
                }

                // Current is a MemberInfo instance which has a good comparer.
                if (xEnumerator.Current != yEnumerator.Current)
                {
                    return false;
                }
            }

            return !yEnumerator.MoveNext();
        }

        public int GetHashCode(MemberExpressionCacheKey obj)
        {
            var hashCodeCombiner = new HashCode();
            hashCodeCombiner.Add(obj.ModelType);

            foreach (var member in obj)
            {
                hashCodeCombiner.Add(member);
            }

            return hashCodeCombiner.ToHashCode();
        }
    }
}
