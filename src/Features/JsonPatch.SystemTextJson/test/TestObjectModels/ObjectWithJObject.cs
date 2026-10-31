// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson;

public class ObjectWithJObject
{
    public JsonObject CustomData { get; set; } = new JsonObject();
}
