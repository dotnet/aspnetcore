// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.JSInterop.Infrastructure
{
    public class JSObjectReferenceJsonConverterTest
    {
        private readonly JSRuntime JSRuntime = new TestJSRuntime();
        private JsonSerializerOptions JsonSerializerOptions => JSRuntime.JsonSerializerOptions;

        [Fact]
        public void Read_Throws_IfJsonIsMissingJSObjectIdProperty()
        {
            // Arrange
            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<JSObjectReference>(json, JsonSerializerOptions));
            Assert.Equal("Required property __jsObjectId not found.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonIsIncomplete()
        {
            // Arrange
            var json = $"{{\"__jsObjectId\":5";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<JSObjectReference>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_Throws_IfJSObjectIdAppearsMultipleTimes()
        {
            // Arrange
            var json = $"{{\"__jsObjectId\":3,\"__jsObjectId\":7}}";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<JSObjectReference>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_ReadsJson()
        {
            // Arrange
            var expectedId = 3;
            var json = $"{{\"__jsObjectId\":{expectedId}}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<JSObjectReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
        }

        [Fact]
        public void Read_ReadsOutermostJSObjectId()
        {
            // Arrange
            var expectedId = 3;
            var json = $"{{\"__jsObjectId\":{expectedId},\"nested\":{{\"__jsObjectId\":7}}}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<JSObjectReference>(json, JsonSerializerOptions);

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
        }

        [Fact]
        public void Write_WritesValidJson()
        {
            // Arrange
            var jsObjectRef = new JSObjectReference(JSRuntime, 7);

            // Act
            var json = JsonSerializer.Serialize(jsObjectRef, JsonSerializerOptions);

            // Assert
            Assert.Equal($"{{\"__jsObjectId\":{jsObjectRef.Id}}}", json);
        }
    }
}
