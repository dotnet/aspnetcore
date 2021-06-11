// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Wasm.Performance.TestApp
{
    [JsonSerializable(typeof(Person), GenerationMode = JsonSourceGenerationMode.MetadataAndSerialization)]
    internal partial class PersonJsonContext : JsonSerializerContext
    {
    }
}
