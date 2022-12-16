// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Wasm.Performance.TestApp;

[JsonSerializable(typeof(Person))]
internal sealed partial class PersonJsonContext : JsonSerializerContext
{
}
