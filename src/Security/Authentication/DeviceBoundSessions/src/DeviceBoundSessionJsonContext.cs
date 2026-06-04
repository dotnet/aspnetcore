// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Source-generated JSON serialization context for DBSC configuration types.
/// </summary>
[JsonSerializable(typeof(DeviceBoundSessionConfiguration))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class DeviceBoundSessionJsonContext : JsonSerializerContext
{
}
