// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

[JsonSerializable(typeof(object[]), GenerationMode = JsonSourceGenerationMode.Serialization)] // JS interop argument lists are always object arrays
internal sealed partial class JSRuntimeSerializerContext : JsonSerializerContext;
