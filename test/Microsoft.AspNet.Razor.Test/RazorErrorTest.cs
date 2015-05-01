// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Razor
{
    public class RazorErrorTest
    {
        [Fact]
        public void RazorError_CanBeSerialized()
        {
            // Arrange
            var error = new RazorError(
                message: "Testing",
                location: new SourceLocation(absoluteIndex: 1, lineIndex: 2, characterIndex: 3),
                length: 456);
            var expectedSerializedError =
                $"{{\"{nameof(RazorError.Message)}\":\"Testing\",\"{nameof(RazorError.Location)}\":{{\"" +
                $"{nameof(SourceLocation.FilePath)}\":null,\"" +
                $"{nameof(SourceLocation.AbsoluteIndex)}\":1,\"{nameof(SourceLocation.LineIndex)}\":2,\"" +
                $"{nameof(SourceLocation.CharacterIndex)}\":3}},\"{nameof(RazorError.Length)}\":456}}";

            // Act
            var serializedError = JsonConvert.SerializeObject(error);

            // Assert
            Assert.Equal(expectedSerializedError, serializedError, StringComparer.Ordinal);
        }

        [Fact]
        public void RazorError_WithFilePath_CanBeSerialized()
        {
            // Arrange
            var error = new RazorError(
                message: "Testing",
                location: new SourceLocation("some-path", absoluteIndex: 1, lineIndex: 2, characterIndex: 56),
                length: 3);
            var expectedSerializedError =
                $"{{\"{nameof(RazorError.Message)}\":\"Testing\",\"{nameof(RazorError.Location)}\":{{\"" +
                $"{nameof(SourceLocation.FilePath)}\":\"some-path\",\"" +
                $"{nameof(SourceLocation.AbsoluteIndex)}\":1,\"{nameof(SourceLocation.LineIndex)}\":2,\"" +
                $"{nameof(SourceLocation.CharacterIndex)}\":56}},\"{nameof(RazorError.Length)}\":3}}";

            // Act
            var serializedError = JsonConvert.SerializeObject(error);

            // Assert
            Assert.Equal(expectedSerializedError, serializedError, StringComparer.Ordinal);
        }

        [Fact]
        public void RazorError_CanBeDeserialized()
        {
            // Arrange
            var error = new RazorError(
                message: "Testing",
                location: new SourceLocation("somepath", absoluteIndex: 1, lineIndex: 2, characterIndex: 3),
                length: 456);
            var serializedError = JsonConvert.SerializeObject(error);

            // Act
            var deserializedError = JsonConvert.DeserializeObject<RazorError>(serializedError);

            // Assert
            Assert.Equal("Testing", deserializedError.Message, StringComparer.Ordinal);
            Assert.Equal(1, deserializedError.Location.AbsoluteIndex);
            Assert.Equal(2, deserializedError.Location.LineIndex);
            Assert.Equal(3, deserializedError.Location.CharacterIndex);
            Assert.Equal(456, deserializedError.Length);
        }
    }
}