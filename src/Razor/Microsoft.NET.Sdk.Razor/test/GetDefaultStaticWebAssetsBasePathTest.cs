// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class GetDefaultStaticWebAssetsBasePathTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void ReturnsError_WhenBasePath_DoesNotContainNonWhitespaceCharacters(string basePath)
        {
            // Arrange
            var expectedError = $"Base path '{basePath ?? "(null)"}' must contain non-whitespace characters.";

            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GetDefaultStaticWebAssetsBasePath
            {
                BuildEngine = buildEngine.Object,
                BasePath = basePath
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal(expectedError, message);
        }

        [Theory]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData(". ")]
        [InlineData(" .")]
        [InlineData(" . ")]
        [InlineData(". .")]
        public void ReturnsError_WhenSafeBasePath_MapsToTheEmptyString(string basePath)
        {
            // Arrange
            var expectedError = $"Base path '{basePath}' must contain non '.' characters.";

            var errorMessages = new List<string>();
            var buildEngine = new Mock<IBuildEngine>();
            buildEngine.Setup(e => e.LogErrorEvent(It.IsAny<BuildErrorEventArgs>()))
                .Callback<BuildErrorEventArgs>(args => errorMessages.Add(args.Message));

            var task = new GetDefaultStaticWebAssetsBasePath
            {
                BuildEngine = buildEngine.Object,
                BasePath = basePath
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.False(result);
            var message = Assert.Single(errorMessages);
            Assert.Equal(expectedError, message);
        }

        [Theory]
        [InlineData("Identity", "identity")]
        [InlineData("Microsoft.AspNetCore.Identity", "microsoftaspnetcoreidentity")]
        public void ReturnsSafeBasePath_WhenBasePath_ContainsUnsafeCharacters(string basePath, string expectedSafeBasePath)
        {
            // Arrange
            var task = new GetDefaultStaticWebAssetsBasePath
            {
                BuildEngine = Mock.Of<IBuildEngine>(),
                BasePath = basePath
            };

            // Act
            var result = task.Execute();

            // Assert
            Assert.True(result);
            Assert.Equal(expectedSafeBasePath, task.SafeBasePath);
        }
    }
}
