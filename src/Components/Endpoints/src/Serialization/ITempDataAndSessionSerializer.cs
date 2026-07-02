// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal interface ITempDataAndSessionSerializer
{
    IDictionary<string, (object? Value, Type? Type)> DeserializeData(IDictionary<string, JsonElement> data);

    byte[] SerializeData(IDictionary<string, (object? Value, Type? Type)> data);

    bool CanSerialize(Type type);

    byte[] SerializeValue(object value, Type type);

    (object? Value, Type? Type) DeserializeValue(ReadOnlySpan<byte> utf8Json);
}
