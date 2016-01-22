// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.JsonPatch.Test.Dynamic
{
    public class NestedDTO
    {
        public string StringProperty { get; set; }
        public dynamic DynamicProperty { get; set; }
    }
}
