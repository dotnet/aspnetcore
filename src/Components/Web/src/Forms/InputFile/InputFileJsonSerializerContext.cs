// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Forms;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    MaxDepth = 32)]
[JsonSerializable(typeof(BrowserFile[]))]
internal partial class InputFileJsonSerializerContext : JsonSerializerContext
{
}
