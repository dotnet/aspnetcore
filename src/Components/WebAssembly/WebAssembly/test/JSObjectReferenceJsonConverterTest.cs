// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;
using Microsoft.JSInterop.WebAssembly;
using Xunit;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    public class JSObjectReferenceJsonConverterTest
    {
        private readonly JsonSerializerOptions JsonSerializerOptions;

        public JSObjectReferenceJsonConverterTest()
        {
            JsonSerializerOptions = DefaultWebAssemblyJSRuntime.Instance.JsonSerializerOptions;
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
            var deserialized = (WebAssemblyJSObjectReference)JsonSerializer.Deserialize<IJSUnmarshalledObjectReference>(json, JsonSerializerOptions)!;

            // Assert
            Assert.Equal(expectedId, deserialized?.Id);
        }
    }
}
