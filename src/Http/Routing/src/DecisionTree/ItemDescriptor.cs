// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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