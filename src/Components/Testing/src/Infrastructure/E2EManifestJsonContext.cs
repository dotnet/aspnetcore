// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

[JsonSerializable(typeof(E2EManifest))]
internal partial class E2EManifestJsonContext : JsonSerializerContext
{
}
