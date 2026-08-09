// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace BlazorServerAotSample;

[JsonSerializable(typeof(Pages.Pages.JsonResolverComposition.InteropOnlyContract))]
internal sealed partial class CompositionInteropJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(Pages.Pages.JsonResolverComposition.StorageOnlyContract))]
internal sealed partial class CompositionStateJsonContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Pages.Pages.JsonResolverComposition.ResolverPrecedencePayload))]
internal sealed partial class ResolverPrecedenceFirstContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Pages.Pages.JsonResolverComposition.ResolverPrecedencePayload))]
internal sealed partial class ResolverPrecedenceSecondContext : JsonSerializerContext;
