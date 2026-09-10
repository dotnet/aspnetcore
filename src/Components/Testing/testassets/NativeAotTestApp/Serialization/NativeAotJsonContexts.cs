// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using NativeAotTestApp.Models;

namespace NativeAotTestApp.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(InteropRequest))]
[JsonSerializable(typeof(InteropResponse))]
[JsonSerializable(typeof(SplitEventArgs))]
[JsonSerializable(typeof(StorageProfile))]
internal sealed partial class NativeAotJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ResolverOrderPayload))]
internal sealed partial class ResolverFirstJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(ResolverOrderPayload))]
internal sealed partial class ResolverSecondJsonContext : JsonSerializerContext;
