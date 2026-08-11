// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace BlazorServerAotSample;

[JsonSerializable(typeof(Pages.Pages.JsonResolverComposition.PartialContractA))]
internal sealed partial class CompositionCoreJsonContext : JsonSerializerContext;
