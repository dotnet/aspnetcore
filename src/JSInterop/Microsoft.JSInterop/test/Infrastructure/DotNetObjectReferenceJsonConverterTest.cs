// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.JSInterop.Infrastructure
{
    public class DotNetObjectReferenceJsonConverterTest
    {
        private readonly JSRuntime JSRuntime = new TestJSRuntime();
        private JsonSerializerOptions JsonSerializerOptions => JSRuntime.JsonSerializerOptions;

        [Fact]
        public void Read_Throws_IfJsonIsMissingDotNetObjectProperty()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dotNetObjectRef = DotNetObjectReference.Create(new TestModel());

            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DotNetObjectReference<TestModel>>(json, JsonSerializerOptions));
            Assert.Equal("Required property __dotNetObject not found.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonContainsUnknownContent()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dotNetObjectRef = DotNetObjectReference.Create(new TestModel());

            var json = "{\"foo\":2}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DotNetObjectReference<TestModel>>(json, JsonSerializerOptions));
            Assert.Equal("Unexcepted JSON property foo.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonIsIncomplete()
        {
            // Arrange
            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectReference.Create(input);
            var objectId = JSRuntime.TrackObjectReference(dotNetObjectRef);

            var json = $"{{\"__dotNetObject\":{objectId}";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<DotNetObjectReference<TestModel>>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_Throws_IfDotNetObjectIdAppearsMultipleTimes()
        {
            // Arrange
            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectReference.Create(input);
            var objectId = JSRuntime.TrackObjectReference(dotNetObjectRef);

            var json = $"{{\"__dotNetObject\":{objectId},\"__dotNetObject\":{objectId}}}";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<DotNetObjectReference<TestModel>>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_ReadsJson()
        {
            // Arrange
            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectReference.Create(input);
            var objectId = JSRuntime.TrackObjectReference(dotNetObjectRef);

            var json = $"{{\"__dotNetObject\":{objectId}}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<DotNetObjectReference<TestModel>>(json, JsonSerializerOptions);

            // Assert
            Assert.Same(input, deserialized.Value);
            Assert.Equal(objectId, deserialized.ObjectId);
        }

        [Fact]
        public void Read_ReturnsTheCorrectInstance()
        {
            // Arrange
            // Track a few instances and verify that the deserialized value returns the correct value.
            var instance1 = new TestModel();
            var instance2 = new TestModel();
            var ref1 = DotNetObjectReference.Create(instance1);
            var ref2 = DotNetObjectReference.Create(instance2);

            var json = $"[{{\"__dotNetObject\":{JSRuntime.TrackObjectReference(ref1)}}},{{\"__dotNetObject\":{JSRuntime.TrackObjectReference(ref2)}}}]";

            // Act
            var deserialized = JsonSerializer.Deserialize<DotNetObjectReference<TestModel>[]>(json, JsonSerializerOptions);

            // Assert
            Assert.Same(instance1, deserialized[0].Value);
            Assert.Same(instance2, deserialized[1].Value);
        }

        [Fact]
        public void Read_ReadsJson_WithFormatting()
        {
            // Arrange
            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectReference.Create(input);
            var objectId = JSRuntime.TrackObjectReference(dotNetObjectRef);

            var json =
@$"{{
    ""__dotNetObject"": {objectId}
}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<DotNetObjectReference<TestModel>>(json, JsonSerializerOptions);

            // Assert
            Assert.Same(input, deserialized.Value);
            Assert.Equal(objectId, deserialized.ObjectId);
        }

        [Fact]
        public void WriteJsonTwice_KeepsObjectId()
        {
            // Arrange
            var dotNetObjectRef = DotNetObjectReference.Create(new TestModel());

            // Act
            var json1 = JsonSerializer.Serialize(dotNetObjectRef, JsonSerializerOptions);
            var json2 = JsonSerializer.Serialize(dotNetObjectRef, JsonSerializerOptions);

            // Assert
            Assert.Equal($"{{\"__dotNetObject\":{dotNetObjectRef.ObjectId}}}", json1);
            Assert.Equal(json1, json2);
        }

        private class TestModel
        {

        }
    }
}
