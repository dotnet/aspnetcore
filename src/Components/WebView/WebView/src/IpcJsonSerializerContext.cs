// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebView;

// Source-generated metadata for every type that crosses the WebView IPC channel, so that IPC works when the
// app is trimmed or published with NativeAOT (where reflection-based serialization is unavailable).
// The options match JsonSerializerOptionsProvider, which IPC used previously, so the wire format is unchanged.
// When adding a new IPC message argument type, it must be added here; IpcCommon.Serialize throws otherwise.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    IncludeFields = true)]
[JsonSerializable(typeof(IEnumerable<object>), TypeInfoPropertyName = "MessageSequence")]
[JsonSerializable(typeof(JsonElement[]))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(byte[]))]
[JsonSerializable(typeof(JSCallResultType))]
[JsonSerializable(typeof(JSCallType))]
[JsonSerializable(typeof(NavigationOptions))]
internal sealed partial class IpcJsonSerializerContext : JsonSerializerContext
{
}
