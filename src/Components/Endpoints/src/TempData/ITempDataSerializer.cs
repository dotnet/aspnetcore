// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal interface ITempDataSerializer
{
    public IDictionary<string, object?> DeserializeData(IDictionary<string, JsonElement> data);

    public byte[] SerializeData(IDictionary<string, object?> data);

    public bool CanSerialize(Type type);
}
