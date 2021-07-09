// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web
{
    public class ClipboardEventArgsReaderTest
    {
        [Fact]
        public void Read_Works()
        {
            // Arrange
            var args = new ClipboardEventArgs
            {
                Type = "Some type"
            };
            var jsonElement = GetJsonElement(args);

            // Act
            var result = ClipboardEventArgsReader.Read(jsonElement);

            // Assert
            Assert.Equal(args.Type, result.Type);
        }

        private static JsonElement GetJsonElement(ClipboardEventArgs args)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
            var jsonReader = new Utf8JsonReader(json);
            var jsonElement = JsonElement.ParseValue(ref jsonReader);
            return jsonElement;
        }
    }
}
