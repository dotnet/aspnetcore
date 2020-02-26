// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.JsonPatch
{
    public class ObjectWithJObject
    {
        public JObject CustomData { get; set; } = new JObject();
    }
}
