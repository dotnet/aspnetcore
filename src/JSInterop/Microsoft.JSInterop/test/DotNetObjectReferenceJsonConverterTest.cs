// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Microsoft.JSInterop.TestJSRuntime;

namespace Microsoft.JSInterop.Tests
{
    public class DotNetObjectReferenceJsonConverterTest
    {
        [Fact]
        public Task Read_Throws_IfJsonIsMissingDotNetObjectProperty() => WithJSRuntime(_ =>
        {
            // Arrange
            var dotNetObjectRef = DotNetObjectRef.Create(new TestModel());

            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DotNetObjectRef<TestModel>>(json));
            Assert.Equal("Required property __dotNetObject not found.", ex.Message);
        });

        [Fact]
        public Task Read_Throws_IfJsonContainsUnknownContent() => WithJSRuntime(_ =>
        {
            // Arrange
            var dotNetObjectRef = DotNetObjectRef.Create(new TestModel());

            var json = "{\"foo\":2}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DotNetObjectRef<TestModel>>(json));
            Assert.Equal("Unexcepted JSON property foo.", ex.Message);
        });

        [Fact]
        public Task Read_ReadsJson() => WithJSRuntime(_ =>
        {
            // Arrange
            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectRef.Create(input);
            var objectId = dotNetObjectRef.ObjectId;

            var json = $"{{\"__dotNetObject\":{objectId}}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<DotNetObjectRef<TestModel>>(json);

            // Assert
            Assert.Same(input, deserialized.Value);
            Assert.Equal(objectId, deserialized.ObjectId);
        });

        [Fact]
        public Task Read_ReturnsTheCorrectInstance() => WithJSRuntime(_ =>
        {
            // Arrange
            // Track a few instances and verify that the deserialized value returns the corect value.
            DotNetObjectRef.Create(new TestModel());
            DotNetObjectRef.Create(new TestModel());

            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectRef.Create(input);
            var objectId = dotNetObjectRef.ObjectId;

            var json = $"{{\"__dotNetObject\":{objectId}}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<DotNetObjectRef<TestModel>>(json);

            // Assert
            Assert.Same(input, deserialized.Value);
            Assert.Equal(objectId, deserialized.ObjectId);
        });

        [Fact]
        public Task Read_ReadsJson_WithFormatting() => WithJSRuntime(_ =>
        {
            // Arrange
            var input = new TestModel();
            var dotNetObjectRef = DotNetObjectRef.Create(input);
            var objectId = dotNetObjectRef.ObjectId;

            var json =
@$"{{
    ""__dotNetObject"": {objectId}
}}";

            // Act
            var deserialized = JsonSerializer.Deserialize<DotNetObjectRef<TestModel>>(json);

            // Assert
            Assert.Same(input, deserialized.Value);
            Assert.Equal(objectId, deserialized.ObjectId);
        });

        [Fact]
        public Task WriteJsonTwice_KeepsObjectId() => WithJSRuntime(_ =>
        {
            // Arrange
            var dotNetObjectRef = DotNetObjectRef.Create(new TestModel());

            // Act
            var json1 = JsonSerializer.Serialize(dotNetObjectRef);
            var json2 = JsonSerializer.Serialize(dotNetObjectRef);

            // Assert
            Assert.Equal($"{{\"__dotNetObject\":{dotNetObjectRef.ObjectId}}}", json1);
            Assert.Equal(json1, json2);
        });

        private class TestModel
        {

        }
    }
}
