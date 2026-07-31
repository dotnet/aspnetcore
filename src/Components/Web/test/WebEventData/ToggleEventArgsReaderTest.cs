// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class ToggleEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        var args = new ToggleEventArgs
        {
            NewState = "open",
            OldState = "closed",
        };

        var jsonElement = GetJsonElement(args);
        var result = ToggleEventArgsReader.Read(jsonElement);

        Assert.Equal(args.NewState, result.NewState);
        Assert.Equal(args.OldState, result.OldState);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);

        return JsonElement.ParseValue(ref jsonReader);
    }
}
