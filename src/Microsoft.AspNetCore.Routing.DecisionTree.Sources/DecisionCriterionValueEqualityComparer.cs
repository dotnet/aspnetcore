// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.DecisionTree
{
    internal class DecisionCriterionValueEqualityComparer : IEqualityComparer<DecisionCriterionValue>
    {
        public DecisionCriterionValueEqualityComparer(IEqualityComparer<object> innerComparer)
        {
            InnerComparer = innerComparer;
        }

        public IEqualityComparer<object> InnerComparer { get; private set; }

        public bool Equals(DecisionCriterionValue x, DecisionCriterionValue y)
        {
            return InnerComparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(DecisionCriterionValue obj)
        {
            return InnerComparer.GetHashCode(obj.Value);
        }
    }
}