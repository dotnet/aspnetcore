// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal sealed record McpAppRequest(string Method, JsonElement Parameters);
internal sealed record McpAppError(int Code, string Message);
internal sealed record McpAppResponse(JsonElement? Result = null, McpAppError? Error = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(McpAppRequest))]
[JsonSerializable(typeof(McpAppResponse))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(McpContentPolicy))]
internal sealed partial class McpAppJsonContext : JsonSerializerContext;
