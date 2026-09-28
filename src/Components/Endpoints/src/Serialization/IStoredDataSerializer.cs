// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal interface IStoredDataSerializer
{
    IDictionary<string, object?> DeserializeData(IDictionary<string, JsonElement> data);

    byte[] SerializeData(IDictionary<string, object?> data);

    bool CanSerialize(Type type);

    byte[] SerializeValue(object value, Type type);

    object? DeserializeValue(ReadOnlySpan<byte> utf8Json, [DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] Type targetType);
}
