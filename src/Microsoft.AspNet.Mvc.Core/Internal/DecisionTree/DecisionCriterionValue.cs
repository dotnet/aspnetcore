// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Internal.DecisionTree
{
    public struct DecisionCriterionValue
    {
        private readonly bool _isCatchAll;
        private readonly object _value;

        public DecisionCriterionValue(object value, bool isCatchAll)
        {
            _value = value;
            _isCatchAll = isCatchAll;
        }

        public bool IsCatchAll
        {
            get { return _isCatchAll; }
        }

        public object Value
        {
            get { return _value; }
        }
    }
}