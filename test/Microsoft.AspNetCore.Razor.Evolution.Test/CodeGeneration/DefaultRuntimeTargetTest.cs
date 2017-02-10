// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class DefaultRuntimeTargetTest
    {
        [Fact]
        public void CreateRenderer_DesignTime_CreatesDesignTimeRenderer()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();
            options.DesignTimeMode = true;

            var target = new DefaultRuntimeTarget(options);

            // Act
            var renderer = target.CreateRenderer(new CSharpRenderingContext());

            // Assert
            Assert.IsType<DesignTimeCSharpRenderer>(renderer);
        }

        [Fact]
        public void CreateRenderer_Runtime_CreatesRuntimeRenderer()
        {
            // Arrange
            var options = RazorParserOptions.CreateDefaultOptions();
            options.DesignTimeMode = false;

            var target = new DefaultRuntimeTarget(options);

            // Act
            var renderer = target.CreateRenderer(new CSharpRenderingContext());

            // Assert
            Assert.IsType<RuntimeCSharpRenderer>(renderer);
        }
    }
}
