// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.DecisionTree
{
    internal interface IClassifier<TItem>
    {
        IDictionary<string, DecisionCriterionValue> GetCriteria(TItem item);

        IEqualityComparer<object> ValueComparer { get; }
    }
}