// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Internal.DecisionTree
{
    public class DecisionCriterionValueEqualityComparer : IEqualityComparer<DecisionCriterionValue>
    {
        public DecisionCriterionValueEqualityComparer(IEqualityComparer<object> innerComparer)
        {
            InnerComparer = innerComparer;
        }

        public IEqualityComparer<object> InnerComparer { get; private set; }

        public bool Equals(DecisionCriterionValue x, DecisionCriterionValue y)
        {
            return x.IsCatchAll == y.IsCatchAll || InnerComparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(DecisionCriterionValue obj)
        {
            if (obj.IsCatchAll)
            {
                return 0;
            }
            else
            {
                return InnerComparer.GetHashCode(obj.Value);
            }
        }
    }
}