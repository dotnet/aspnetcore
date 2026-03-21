// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]

// Required for WebAssemblyComponentParameterDeserializer
[JsonSerializable(typeof(ComponentParameter[]))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(IList<object>))]

// Required for DefaultWebAssemblyJSRuntime
[JsonSerializable(typeof(RootComponentOperationBatch))]
internal sealed partial class WebAssemblyJsonSerializerContext : JsonSerializerContext;
