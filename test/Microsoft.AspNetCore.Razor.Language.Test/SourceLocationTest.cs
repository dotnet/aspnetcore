// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class SourceLocationTest
    {
        [Fact]
        public void ConstructorWithLineAndCharacterIndexSetsAssociatedProperties()
        {
            // Act
            var loc = new SourceLocation(0, 42, 24);

            // Assert
            Assert.Null(loc.FilePath);
            Assert.Equal(0, loc.AbsoluteIndex);
            Assert.Equal(42, loc.LineIndex);
            Assert.Equal(24, loc.CharacterIndex);
        }

        [Fact]
        public void Constructor_SetsFilePathAndIndexes()
        {
            // Arrange
            var filePath = "some-file-path";
            var absoluteIndex = 133;
            var lineIndex = 23;
            var characterIndex = 12;

            // Act
            var sourceLocation = new SourceLocation(filePath, absoluteIndex, lineIndex, characterIndex);

            // Assert
            Assert.Equal(filePath, sourceLocation.FilePath);
            Assert.Equal(absoluteIndex, sourceLocation.AbsoluteIndex);
            Assert.Equal(lineIndex, sourceLocation.LineIndex);
            Assert.Equal(characterIndex, sourceLocation.CharacterIndex);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("some-file")]
        public void GetHashCode_ReturnsSameValue_WhenEqual(string path)
        {
            // Arrange
            var sourceLocationA = new SourceLocation(path, 10, 3, 4);
            var sourceLocationB = new SourceLocation(path, 10, 3, 4);
            var sourceLocationC = new SourceLocation(path, 10, 45, 8754);

            // Act
            var hashCodeA = sourceLocationA.GetHashCode();
            var hashCodeB = sourceLocationB.GetHashCode();
            var hashCodeC = sourceLocationC.GetHashCode();

            // Assert
            Assert.Equal(hashCodeA, hashCodeB);
            Assert.Equal(hashCodeA, hashCodeC);
        }

        [Fact]
        public void Equals_ReturnsTrue_FilePathsNullAndAbsoluteIndicesMatch()
        {
            // Arrange
            var sourceLocationA = new SourceLocation(10, 3, 4);
            var sourceLocationB = new SourceLocation(10, 45, 8754);

            // Act
            var result = sourceLocationA.Equals(sourceLocationB);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Equals_ReturnsFalse_IfFilePathIsDifferent()
        {
            // Arrange
            var sourceLocationA = new SourceLocation(10, 3, 4);
            var sourceLocationB = new SourceLocation("different-file", 10, 3, 4);

            // Act
            var result = sourceLocationA.Equals(sourceLocationB);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("some-file")]
        public void Equals_ReturnsTrue_IfFilePathAndIndexesAreSame(string path)
        {
            // Arrange
            var sourceLocationA = new SourceLocation(path, 10, 3, 4);
            var sourceLocationB = new SourceLocation(path, 10, 3, 4);
            var sourceLocationC = new SourceLocation("different-path", 10, 3, 4);

            // Act
            var result1 = sourceLocationA.Equals(sourceLocationB);
            var result2 = sourceLocationA.Equals(sourceLocationC);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }
    }
}
