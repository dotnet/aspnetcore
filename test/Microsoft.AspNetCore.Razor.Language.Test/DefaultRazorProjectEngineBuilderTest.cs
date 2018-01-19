// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorProjectEngineBuilderTest
    {
        [Fact]
        public void Build_AddsFeaturesToRazorEngine()
        {
            // Arrange
            var builder = new DefaultRazorProjectEngineBuilder(false, Mock.Of<RazorProjectFileSystem>());
            builder.Features.Add(Mock.Of<IRazorEngineFeature>());
            builder.Features.Add(Mock.Of<IRazorEngineFeature>());
            builder.Features.Add(Mock.Of<IRazorProjectEngineFeature>());

            var features = builder.Features.ToArray();

            // Act
            var projectEngine = builder.Build();

            // Assert
            Assert.Collection(projectEngine.Engine.Features,
                feature => Assert.Same(features[0], feature),
                feature => Assert.Same(features[1], feature));
        }

        [Fact]
        public void Build_AddsPhasesToRazorEngine()
        {
            // Arrange
            var builder = new DefaultRazorProjectEngineBuilder(false, Mock.Of<RazorProjectFileSystem>());
            builder.Phases.Add(Mock.Of<IRazorEnginePhase>());
            builder.Phases.Add(Mock.Of<IRazorEnginePhase>());

            var phases = builder.Phases.ToArray();

            // Act
            var projectEngine = builder.Build();

            // Assert
            Assert.Collection(projectEngine.Engine.Phases,
                phase => Assert.Same(phases[0], phase),
                phase => Assert.Same(phases[1], phase));
        }

        [Fact]
        public void Build_CreatesProjectEngineWithFileSystem()
        {
            // Arrange
            var fileSystem = Mock.Of<RazorProjectFileSystem>();
            var builder = new DefaultRazorProjectEngineBuilder(false, fileSystem);

            // Act
            var projectEngine = builder.Build();

            // Assert
            Assert.Same(fileSystem, projectEngine.FileSystem);
        }
    }
}
