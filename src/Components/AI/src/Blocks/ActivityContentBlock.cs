// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI;

public class ActivityContentBlock : ContentBlock
{
    public string ActivityType { get; set; } = string.Empty;

    public JsonElement Content { get; set; }
}
