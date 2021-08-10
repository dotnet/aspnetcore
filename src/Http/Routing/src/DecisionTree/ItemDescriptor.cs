// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.DecisionTree
{
    internal class ItemDescriptor<TItem>
    {
        public IDictionary<string, DecisionCriterionValue> Criteria { get; set; }

        public int Index { get; set; }

        public TItem Item { get; set; }
    }
}
