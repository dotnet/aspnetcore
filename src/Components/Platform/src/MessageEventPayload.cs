// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Platform;

internal sealed class MessageEventPayload
{
    public JsonElement Data { get; set; }

    public string Origin { get; set; } = "";

    internal MessageEvent ToMessageEvent()
    {
        if (Data.TryGetProperty("error", out var error))
        {
            throw new JsonException(error.GetString());
        }

        using var document = JsonDocument.Parse(Data.GetProperty("json").GetString()!);

        return new MessageEvent { Data = document.RootElement.Clone(), Origin = Origin };
    }
}
