// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultRazorEngineBuilderTest
    {
        [Fact]
        public void Build_AddsFeaturesAndPhases()
        {
            // Arrange
            var builder = new DefaultRazorEngineBuilder();

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
