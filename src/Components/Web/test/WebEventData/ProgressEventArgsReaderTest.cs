// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class ProgressEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new ProgressEventArgs
        {
            LengthComputable = true,
            Loaded = 8,
            Total = 91,
            Type = "progress1,"
        };
        var jsonElement = GetJsonElement(args);

        // Act
        var result = ProgressEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.LengthComputable, result.LengthComputable);
        Assert.Equal(args.Loaded, result.Loaded);
        Assert.Equal(args.Total, result.Total);
        Assert.Equal(args.Type, result.Type);
    }

    private static JsonElement GetJsonElement<T>(T args)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
        var jsonReader = new Utf8JsonReader(json);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);
        return jsonElement;
    }
}
