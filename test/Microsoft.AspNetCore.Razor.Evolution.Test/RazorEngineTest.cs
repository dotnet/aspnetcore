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
        public void Create_NoArg_CreatesDefaultRuntimeEngine()
        {
            // Arrange
            // Act
            var engine = RazorEngine.Create();

            // Assert
            Assert.IsType<DefaultRazorEngine>(engine);
            AssertDefaultRuntimeFeatures(engine.Features);
            AssertDefaultRuntimePhases(engine.Phases);
        }

        [Fact]
        public void CreateDesignTime_NoArg_CreatesDefaultDesignTimeEngine()
        {
            // Arrange
            // Act
            var engine = RazorEngine.CreateDesignTime();

            // Assert
            Assert.IsType<DefaultRazorEngine>(engine);
            AssertDefaultDesignTimeFeatures(engine.Features);
            AssertDefaultDesignTimePhases(engine.Phases);
        }

        [Fact]
        public void Create_Null_CreatesDefaultRuntimeEngine()
        {
            // Arrange
            // Act
            var engine = RazorEngine.Create(configure: null);

            // Assert
            Assert.IsType<DefaultRazorEngine>(engine);
            AssertDefaultRuntimeFeatures(engine.Features);
            AssertDefaultRuntimePhases(engine.Phases);
        }

        [Fact]
        public void CreateDesignTime_Null_CreatesDefaultDesignTimeEngine()
        {
            // Arrange
            // Act
            var engine = RazorEngine.CreateDesignTime(configure: null);

            // Assert
            Assert.IsType<DefaultRazorEngine>(engine);
            AssertDefaultDesignTimeFeatures(engine.Features);
            AssertDefaultDesignTimePhases(engine.Phases);
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

        [Fact]
        public void CreateDesignTime_Lambda_AddsFeaturesAndPhases()
        {
            // Arrange
            IRazorEngineFeature[] features = null;
            IRazorEnginePhase[] phases = null;

            // Act
            var engine = RazorEngine.CreateDesignTime(builder =>
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

        private static void AssertDefaultRuntimeFeatures(IEnumerable<IRazorEngineFeature> features)
        {
            Assert.Collection(
                features,
                feature => Assert.IsType<DefaultDirectiveSyntaxTreePass>(feature),
                feature => Assert.IsType<HtmlNodeOptimizationPass>(feature),
                feature => Assert.IsType<TagHelperBinderSyntaxTreePass>(feature),
                feature => Assert.IsType<DefaultDocumentClassifier>(feature),
                feature => Assert.IsType<DefaultDirectiveIRPass>(feature));
        }

        private static void AssertDefaultRuntimePhases(IReadOnlyList<IRazorEnginePhase> phases)
        {
            Assert.Collection(
                phases,
                phase => Assert.IsType<DefaultRazorParsingPhase>(phase),
                phase => Assert.IsType<DefaultRazorSyntaxTreePhase>(phase),
                phase => Assert.IsType<DefaultRazorIRLoweringPhase>(phase),
                phase => Assert.IsType<DefaultRazorIRPhase>(phase),
                phase => Assert.IsType<DefaultRazorRuntimeCSharpLoweringPhase>(phase));
        }

        private static void AssertDefaultDesignTimeFeatures(IEnumerable<IRazorEngineFeature> features)
        {
            Assert.Collection(
                features,
                feature => Assert.IsType<DefaultDirectiveSyntaxTreePass>(feature),
                feature => Assert.IsType<HtmlNodeOptimizationPass>(feature),
                feature => Assert.IsType<TagHelperBinderSyntaxTreePass>(feature),
                feature => Assert.IsType<DefaultDocumentClassifier>(feature),
                feature => Assert.IsType<DefaultDirectiveIRPass>(feature),
                feature => Assert.IsType<RazorEngine.ConfigureDesignTimeOptions>(feature),
                feature => Assert.IsType<RazorDesignTimeIRPass>(feature));
        }

        private static void AssertDefaultDesignTimePhases(IReadOnlyList<IRazorEnginePhase> phases)
        {
            Assert.Collection(
                phases,
                phase => Assert.IsType<DefaultRazorParsingPhase>(phase),
                phase => Assert.IsType<DefaultRazorSyntaxTreePhase>(phase),
                phase => Assert.IsType<DefaultRazorIRLoweringPhase>(phase),
                phase => Assert.IsType<DefaultRazorIRPhase>(phase),
                phase => Assert.IsType<DefaultRazorDesignTimeCSharpLoweringPhase>(phase));
        }
    }
}