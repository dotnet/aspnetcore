// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal abstract class SettingsConverterBase<T> : JsonConverter<T>
{
    public SettingsConverterBase(JsonContext context)
    {
        Context = context;
    }

    public JsonContext Context { get; }
}
