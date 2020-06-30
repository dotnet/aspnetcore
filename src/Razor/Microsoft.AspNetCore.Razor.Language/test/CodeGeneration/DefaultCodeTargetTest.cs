// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DefaultCodeTargetTest
    {
        [Fact]
        public void Constructor_CreatesDefensiveCopy()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();

            var extensions = new ICodeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
            };

            // Act
            var target = new DefaultCodeTarget(options, extensions);

            // Assert
            Assert.NotSame(extensions, target);
        }

        [Fact]
        public void CreateWriter_DesignTime_CreatesDesignTimeNodeWriter()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDesignTimeDefault();
            var target = new DefaultCodeTarget(options, Enumerable.Empty<ICodeTargetExtension>());

            // Act
            var writer = target.CreateNodeWriter();

            // Assert
            Assert.IsType<DesignTimeNodeWriter>(writer);
        }

        [Fact]
        public void CreateWriter_Runtime_CreatesRuntimeNodeWriter()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();
            var target = new DefaultCodeTarget(options, Enumerable.Empty<ICodeTargetExtension>());

            // Act
            var writer = target.CreateNodeWriter();

            // Assert
            Assert.IsType<RuntimeNodeWriter>(writer);
        }

        [Fact]
        public void HasExtension_ReturnsTrue_WhenExtensionFound()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();

            var extensions = new ICodeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
            };

            var target = new DefaultCodeTarget(options, extensions);

            // Act
            var result = target.HasExtension<MyExtension1>();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasExtension_ReturnsFalse_WhenExtensionNotFound()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();

            var extensions = new ICodeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension2(),
            };

            var target = new DefaultCodeTarget(options, extensions);

            // Act
            var result = target.HasExtension<MyExtension1>();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetExtension_ReturnsExtension_WhenExtensionFound()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();

            var extensions = new ICodeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
            };

            var target = new DefaultCodeTarget(options, extensions);

            // Act
            var result = target.GetExtension<MyExtension1>();

            // Assert
            Assert.Same(extensions[1], result);
        }

        [Fact]
        public void GetExtension_ReturnsFirstMatch_WhenExtensionFound()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();

            var extensions = new ICodeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension1(),
                new MyExtension2(),
                new MyExtension1(),
            };

            var target = new DefaultCodeTarget(options, extensions);

            // Act
            var result = target.GetExtension<MyExtension1>();

            // Assert
            Assert.Same(extensions[1], result);
        }


        [Fact]
        public void GetExtension_ReturnsNull_WhenExtensionNotFound()
        {
            // Arrange
            var options = RazorCodeGenerationOptions.CreateDefault();

            var extensions = new ICodeTargetExtension[]
            {
                new MyExtension2(),
                new MyExtension2(),
            };

            var target = new DefaultCodeTarget(options, extensions);

            // Act
            var result = target.GetExtension<MyExtension1>();

            // Assert
            Assert.Null(result);
        }

        private class MyExtension1 : ICodeTargetExtension
        {
        }

        private class MyExtension2 : ICodeTargetExtension
        {
        }
    }
}
