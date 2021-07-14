// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web
{
    public class ChangeEventArgsReaderTest
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Read_WithBoolValue(bool changeValue)
        {
            // Arrange
            var args = new ChangeEventArgs
            {
                Value = changeValue,
            };
            var jsonElement = GetJsonElement(args);

            // Act
            var result = ChangeEventArgsReader.Read(jsonElement);

            // Assert
            Assert.Equal(args.Value, result.Value);
        }

        [Fact]
        public void Read_WithNullValue()
        {
            // Arrange
            var args = new ChangeEventArgs
            {
                Value = null,
            };
            var jsonElement = GetJsonElement(args);

            // Act
            var result = ChangeEventArgsReader.Read(jsonElement);

            // Assert
            Assert.Equal(args.Value, result.Value);
        }

        [Fact]
        public void Read_WithStringValue()
        {
            // Arrange
            var args = new ChangeEventArgs
            {
                Value = "Hello world",
            };
            var jsonElement = GetJsonElement(args);

            // Act
            var result = ChangeEventArgsReader.Read(jsonElement);

            // Assert
            Assert.Equal(args.Value, result.Value);
        }

        private static JsonElement GetJsonElement(ChangeEventArgs args)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
            var jsonReader = new Utf8JsonReader(json);
            var jsonElement = JsonElement.ParseValue(ref jsonReader);
            return jsonElement;
        }
    }
}
