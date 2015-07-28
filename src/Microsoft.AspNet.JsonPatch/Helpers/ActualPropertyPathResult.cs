// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.JsonPatch.Helpers
{
    internal class ActualPropertyPathResult
    {
        public int NumericEnd { get; private set; }
        public string PathToProperty { get; set; }
        public bool ExecuteAtEnd { get; set; }

        public ActualPropertyPathResult(
            int numericEnd,
            string pathToProperty,
            bool executeAtEnd)
        {         
            NumericEnd = numericEnd;
            PathToProperty = pathToProperty;
            ExecuteAtEnd = executeAtEnd;
        }
    }
}
