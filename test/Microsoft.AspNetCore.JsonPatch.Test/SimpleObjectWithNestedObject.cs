// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class SimpleObjectWithNestedObject
    {
        public int IntegerValue { get; set; }

        public NestedObject NestedObject { get; set; }

        public SimpleObject SimpleObject { get; set; }

        public InheritedObject InheritedObject { get; set; }

        public List<SimpleObject> SimpleObjectList { get; set; }

        public IList<SimpleObject> SimpleObjectIList { get; set; }

        public SimpleObjectWithNestedObject()
        {
            NestedObject = new NestedObject();
            SimpleObject = new SimpleObject();
            InheritedObject = new InheritedObject();
            SimpleObjectList = new List<SimpleObject>();
        }
    }
}