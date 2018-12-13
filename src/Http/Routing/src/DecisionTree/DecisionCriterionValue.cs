// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.DecisionTree
{
    internal struct DecisionCriterionValue
    {
        private readonly object _value;

        public DecisionCriterionValue(object value)
        {
            _value = value;
        }

        public object Value
        {
            get { return _value; }
        }
    }
}