// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

// JS interop argument lists are always object arrays
[JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Serialization)]
internal sealed partial class JSRuntimeSerializerContext : JsonSerializerContext;
