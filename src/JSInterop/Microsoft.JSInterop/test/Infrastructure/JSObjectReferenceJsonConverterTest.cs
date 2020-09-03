// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Microsoft.JSInterop.Implementation;
using Xunit;

namespace Microsoft.JSInterop.Infrastructure
{
    public class JSObjectReferenceJsonConverterTest
    {
        private readonly JSRuntime JSRuntime = new TestJSRuntime();
        private readonly JsonSerializerOptions JsonSerializerOptions;

        public JSObjectReferenceJsonConverterTest()
        {
            JsonSerializerOptions = JSRuntime.JsonSerializerOptions;
            JsonSerializerOptions.Converters.Add(new JSObjectReferenceJsonConverter<IJSInProcessObjectReference, JSInProcessObjectReference>(
                id => new JSInProcessObjectReference(default!, id)));
            JsonSerializerOptions.Converters.Add(new JSObjectReferenceJsonConverter<IJSUnmarshalledObjectReference, TestJSUnmarshalledObjectReference>(
                id => new TestJSUnmarshalledObjectReference(id)));
        }

        [Fact]
        public void Read_Throws_IfJsonIsMissingJSObjectIdProperty()
        {
            // Arrange
            var json = "{}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
            Assert.Equal("Required property __jsObjectId not found.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonContainsUnknownContent()
        {
            // Arrange
            var json = "{\"foo\":2}";

            // Act & Assert
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
            Assert.Equal("Unexcepted JSON property foo.", ex.Message);
        }

        [Fact]
        public void Read_Throws_IfJsonIsIncomplete()
        {
            // Arrange
            var json = $"{{\"__jsObjectId\":5";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_Throws_IfJSObjectIdAppearsMultipleTimes()
        {
            // Arrange
            var json = $"{{\"__jsObjectId\":3,\"__jsObjectId\":7}}";

            // Act & Assert
            var ex = Record.Exception(() => JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions));
            Assert.IsAssignableFrom<JsonException>(ex);
        }

        [Fact]
        public void Read_ReadsJson_IJSObjectReference()
        {
            // Arrange
            var expectedId = 3;
            var json = $"{{\"__jsObjectId\":{expectedId}}}";

            // Act
            var deserialized = (JSObjectReference)JsonSerializer.Deserialize<IJSObjectReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
        }

        [Fact]
        public void Read_ReadsJson_IJSInProcessObjectReference()
        {
            // Arrange
            var expectedId = 3;
            var json = $"{{\"__jsObjectId\":{expectedId}}}";

            // Act
            var deserialized = (JSInProcessObjectReference)JsonSerializer.Deserialize<IJSInProcessObjectReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
        }

        [Fact]
        public void Read_ReadsJson_IJSUnmarshalledObjectReference()
        {
            // Arrange
            var expectedId = 3;
            var json = $"{{\"__jsObjectId\":{expectedId}}}";

            // Act
            var deserialized = (TestJSUnmarshalledObjectReference)JsonSerializer.Deserialize<IJSUnmarshalledObjectReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
        }

        [Fact]
        public void Write_WritesValidJson()
        {
            // Arrange
            var jsObjectRef = new JSObjectReference(JSRuntime, 7);

            // Act
            var json = JsonSerializer.Serialize((IJSObjectReference)jsObjectRef, JsonSerializerOptions);

            // Assert
            Assert.Equal($"{{\"__jsObjectId\":{jsObjectRef.Id}}}", json);
        }

        private class TestJSUnmarshalledObjectReference : JSInProcessObjectReference, IJSUnmarshalledObjectReference
        {
            public TestJSUnmarshalledObjectReference(long id) : base(default!, id)
            {
            }

            public TResult InvokeUnmarshalled<TResult>(string identifier)
            {
                throw new NotImplementedException();
            }

            public TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
            {
                throw new NotImplementedException();
            }

            public TResult InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1)
            {
                throw new NotImplementedException();
            }

            public TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
            {
                throw new NotImplementedException();
            }
        }
    }
}
