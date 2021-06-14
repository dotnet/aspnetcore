// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.JSInterop.Implementation;
using Xunit;

namespace Microsoft.JSInterop.Infrastructure
{
    public class JSDataReferenceJsonConverterTest
    {
        private readonly JSRuntime JSRuntime = new TestJSRuntime();
        private readonly JsonSerializerOptions JsonSerializerOptions;

        public JSDataReferenceJsonConverterTest()
        {
            JsonSerializerOptions = JSRuntime.JsonSerializerOptions;
            JsonSerializerOptions.Converters.Add(new JSDataReferenceJsonConverter(JSRuntime));
        }

        [Fact]
        public void Read_Throws_IfJsonIsMissingJSObjectIdProperty()
        {
            // Arrange
            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions));
            Assert.Equal("Required property __jsObjectId not found.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonContainsUnknownContent()
        {
            // Arrange
            var json = "{\"foo\":2}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions));
            Assert.Equal("Unexcepted JSON property foo.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonIsIncomplete()
        {
            // Arrange
            var json = $"{{\"__jsObjectId\":5";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_Throws_IfJSObjectIdAppearsMultipleTimes()
        {
            // Arrange
            var json = $"{{\"__jsObjectId\":3,\"__jsObjectId\":7}}";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_Throws_IfLengthNotProvided()
        {
            // Arrange
            var expectedId = 3;
            var json = $"{{\"__jsObjectId\":{expectedId}}}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions));
            Assert.Equal("Required property __jsDataReferenceLength not found.", ex.Message);
        }

        [Fact]
        public void Read_ReadsJson_IJSDataReference()
        {
            // Arrange
            var expectedId = 3;
            var expectedLength = 5;
            var json = $"{{\"__jsObjectId\":{expectedId}, \"__jsDataReferenceLength\":{expectedLength}}}";

            // Act
            var deserialized = (JSDataReference)JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
            Assert.Equal(expectedLength, deserialized?.Length);
        }

        [Fact]
        public void Read_ReadsJson_IJSDataReferenceReverseOrder()
        {
            // Arrange
            var expectedId = 3;
            var expectedLength = 5;
            var json = $"{{\"__jsDataReferenceLength\":{expectedLength}, \"__jsObjectId\":{expectedId}}}";

            // Act
            var deserialized = (JSDataReference)JsonSerializer.Deserialize<IJSDataReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
            Assert.Equal(expectedLength, deserialized?.Length);
        }

        [Fact]
        public void Write_WritesValidJson()
        {
            // Arrange
            var jsObjectRef = new JSDataReference(JSRuntime, 7, 10);

            // Act
            var json = JsonSerializer.Serialize((IJSDataReference)jsObjectRef, JsonSerializerOptions);

            // Assert
            Assert.Equal($"{{\"__jsObjectId\":{jsObjectRef.Id}}}", json);
        }
    }
}
