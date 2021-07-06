// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web
{
    public class FocusEventArgsReaderTest
    {
        [Fact]
        public void Read_Works()
        {
            // Arrange
            var args = new FocusEventArgs
            {
                Type = "type1",
            };
           
            var jsonElement = GetJsonElement(args);

            // Act
            var result = FocusEventArgsReader.Read(jsonElement);

            // Assert
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
}
