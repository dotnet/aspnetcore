// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class RazorEngineTest
    {
        [Fact]
        public void Create_NoArg_CreatesDefaultEngine()
        {
            // Arrange
            // Act
            var engine = RazorEngine.Create();

            // Assert
            Assert.IsType<DefaultRazorEngine>(engine);
            AssertDefaultFeatures(engine.Features);
            AssertDefaultPhases(engine.Phases);
        }

        [Fact]
        public void Create_Null_CreatesDefaultEngine()
        {
            // Arrange
            // Act
            var engine = RazorEngine.Create(configure: null);

            // Assert
            Assert.IsType<DefaultRazorEngine>(engine);
            AssertDefaultFeatures(engine.Features);
            AssertDefaultPhases(engine.Phases);
        }

        [Fact]
        public void Create_Lambda_AddsFeaturesAndPhases()
        {
            // Arrange
            IRazorEngineFeature[] features = null;
            IRazorEnginePhase[] phases = null;

            // Act
            var engine = RazorEngine.Create(builder =>
            {
                builder.Features.Clear();
                builder.Phases.Clear();

                builder.Features.Add(Mock.Of<IRazorEngineFeature>());
                builder.Features.Add(Mock.Of<IRazorEngineFeature>());

                builder.Phases.Add(Mock.Of<IRazorEnginePhase>());
                builder.Phases.Add(Mock.Of<IRazorEnginePhase>());

                features = builder.Features.ToArray();
                phases = builder.Phases.ToArray();
            });

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

        private static void AssertDefaultFeatures(IEnumerable<IRazorEngineFeature> features)
        {
            Assert.Collection(
                features,
                feature => Assert.IsType<DefaultDirectiveSyntaxTreePass>(feature),
                feature => Assert.IsType<TagHelperBinderSyntaxTreePass>(feature),
                feature => Assert.IsType<HtmlNodeOptimizationPass>(feature),
                feature => Assert.IsType<DefaultDirectiveIRPass>(feature));
        }

        private static void AssertDefaultPhases(IReadOnlyList<IRazorEnginePhase> phases)
        {
            Assert.Collection(
                phases,
                phase => Assert.IsType<DefaultRazorParsingPhase>(phase),
                phase => Assert.IsType<DefaultRazorSyntaxTreePhase>(phase),
                phase => Assert.IsType<DefaultRazorIRLoweringPhase>(phase),
                phase => Assert.IsType<DefaultRazorIRPhase>(phase),
                phase => Assert.IsType<DefaultRazorCSharpLoweringPhase>(phase));
        }
    }
}