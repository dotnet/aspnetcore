// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DefaultRuntimeTargetTest
    {
        [Fact]
        public void Constructor_CreatesDefensiveCopy()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
            };

            // Act
            var target = new DefaultRuntimeTarget(options, extensions);

            // Assert
            Assert.NotSame(extensions, target);
        }

        [Fact]
        public void CreateWriter_CreatesDefaultDocumentWriter()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var target = new DefaultRuntimeTarget(options, Enumerable.Empty<IRuntimeTargetExtension>());

            // Act
            var writer = target.CreateWriter(new CSharpRenderingContext());

            // Assert
            Assert.IsType<DefaultDocumentWriter>(writer);
        }

        [Fact]
        public void HasExtension_ReturnsTrue_WhenExtensionFound()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
            };

            var target = new DefaultRuntimeTarget(options, extensions);

            // Act
            var result = target.HasExtension<MyExtension1>();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasExtension_ReturnsFalse_WhenExtensionNotFound()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension2(),
            };

            var target = new DefaultRuntimeTarget(options, extensions);

            // Act
            var result = target.HasExtension<MyExtension1>();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetExtension_ReturnsExtension_WhenExtensionFound()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
            };

            var target = new DefaultRuntimeTarget(options, extensions);

            // Act
            var result = target.GetExtension<MyExtension1>();

            // Assert
            Assert.Same(extensions[1], result);
        }

        [Fact]
        public void GetExtension_ReturnsFirstMatch_WhenExtensionFound()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
                new MyExtension2(),
                new MyExtension1(),
            };

            var target = new DefaultRuntimeTarget(options, extensions);

            // Act
            var result = target.GetExtension<MyExtension1>();

            // Assert
            Assert.Same(extensions[1], result);
        }


        [Fact]
        public void GetExtension_ReturnsNull_WhenExtensionNotFound()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();

            var extensions = new IRuntimeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension2(),
            };

            var target = new DefaultRuntimeTarget(options, extensions);

            // Act
            var result = target.GetExtension<MyExtension1>();

            // Assert
            Assert.Null(result);
        }

        private class MyExtension1 : IRuntimeTargetExtension
        {
        }

        private class MyExtension2 : IRuntimeTargetExtension
        {
        }
    }
}
