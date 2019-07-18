// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class ElementReferenceTest
    {
        [Fact]
        public void Serializing_Works()
        {
            // Arrange
            var elementReference = ElementReference.CreateWithUniqueId();
            var expected = $"{{\"__internalId\":\"{elementReference.Id}\"}}";

            // Act
            var json = JsonSerializer.Serialize(elementReference, JsonSerializerOptionsProvider.Options);

            // Assert
            Assert.Equal(expected, json);
        }

        [Fact]
        public void Deserializing_Works()
        {
            // Arrange
            var id = ElementReference.CreateWithUniqueId().Id;
            var json = $"{{\"__internalId\":\"{id}\"}}";

            // Act
            var elementReference = JsonSerializer.Deserialize<ElementReference>(json, JsonSerializerOptionsProvider.Options);

            // Assert
            Assert.Equal(id, elementReference.Id);
        }

        [Fact]
        public void Deserializing_WithFormatting_Works()
        {
            // Arrange
            var id = ElementReference.CreateWithUniqueId().Id;
            var json =
@$"{{
    ""__internalId"": ""{id}""
}}";

            // Act
            var elementReference = JsonSerializer.Deserialize<ElementReference>(json, JsonSerializerOptionsProvider.Options);

            // Assert
            Assert.Equal(id, elementReference.Id);
        }

        [Fact]
        public void Deserializing_Throws_IfUnknownPropertyAppears()
        {
            // Arrange
            var json = "{\"id\":\"some-value\"}";

            // Act
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ElementReference>(json, JsonSerializerOptionsProvider.Options));

            // Assert
            Assert.Equal("Unexpected JSON property 'id'.", ex.Message);
        }

        [Fact]
        public void Deserializing_Throws_IfIdIsNotSpecified()
        {
            // Arrange
            var json = "{}";

            // Act
            var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ElementReference>(json, JsonSerializerOptionsProvider.Options));

            // Assert
            Assert.Equal("__internalId is required.", ex.Message);
        }
    }
}
