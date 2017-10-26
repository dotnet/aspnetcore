// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class SimpleObject
    {
        public List<SimpleObject> SimpleObjectList { get; set; }
        public List<int> IntegerList { get; set; }
        public IList<int> IntegerIList { get; set; }
        public int IntegerValue { get; set; }
        public int AnotherIntegerValue { get; set; }
        public string StringProperty { get; set; }
        public string AnotherStringProperty { get; set; }
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public float FloatValue { get; set; }
        public Guid GuidValue { get; set; }
    }
}