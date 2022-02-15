// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

internal abstract class SettingsConverterBase<T> : JsonConverter<T>
{
    public SettingsConverterBase(JsonSettings settings)
    {
        Settings = settings;
    }

    public JsonSettings Settings { get; }
}
