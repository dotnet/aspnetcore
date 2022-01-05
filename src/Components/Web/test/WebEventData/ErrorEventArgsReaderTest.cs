// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Web;

public class ErrorEventArgsReaderTest
{
    [Fact]
    public void Read_Works()
    {
        // Arrange
        var args = new ErrorEventArgs
        {
            Colno = 3,
            Filename = "test",
            Lineno = 8,
            Message = "Error1",
            Type = "type2",
        };

        var jsonElement = GetJsonElement(args);

        // Act
        var result = ErrorEventArgsReader.Read(jsonElement);

        // Assert
        Assert.Equal(args.Colno, result.Colno);
        Assert.Equal(args.Filename, result.Filename);
        Assert.Equal(args.Lineno, result.Lineno);
        Assert.Equal(args.Message, result.Message);
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
