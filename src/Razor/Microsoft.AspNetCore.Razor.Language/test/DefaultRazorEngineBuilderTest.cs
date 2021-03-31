// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorEngineBuilderTest
    {
        [Fact]
        public void Build_AddsFeaturesAndPhases()
        {
            // Arrange
            var builder = new DefaultRazorEngineBuilder(designTime: false);

            builder.Features.Add(Mock.Of<IRazorEngineFeature>());
            builder.Features.Add(Mock.Of<IRazorEngineFeature>());

            builder.Phases.Add(Mock.Of<IRazorEnginePhase>());
            builder.Phases.Add(Mock.Of<IRazorEnginePhase>());

            var features = builder.Features.ToArray();
            var phases = builder.Phases.ToArray();

            // Act
            var engine = builder.Build();

            // Assert
            Assert.Collection(
                engine.Features,
                f => Assert.Same(features[0], f),
                f => Assert.Same(features[1], f));

            Assert.Collection(
                engine.Phases,
                p => Assert.Same(phases[0], p),
                p => Assert.Same(phases[1], p));
        }
    }
}
