// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.JsonPatch;

public class ObjectWithJObject
{
    public JObject CustomData { get; set; } = new JObject();
}
