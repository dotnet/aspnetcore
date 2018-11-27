// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class RazorProjectEngineTest
    {
        [Fact]
        public void CreateDesignTime_Lambda_AddsFeaturesAndPhases()
        {
            // Arrange

            // Act
            var engine = RazorProjectEngine.Create(RazorConfiguration.Default, Mock.Of<RazorProjectFileSystem>());

            // Assert
            AssertDefaultPhases(engine);
            AssertDefaultFeatures(engine);
            AssertDefaultDirectives(engine);
            AssertDefaultTargetExtensions(engine);
        }

        private static void AssertDefaultPhases(RazorProjectEngine engine)
        {
            Assert.Collection(
                engine.Phases,
                phase => Assert.IsType<DefaultRazorParsingPhase>(phase),
                phase => Assert.IsType<DefaultRazorSyntaxTreePhase>(phase),
                phase => Assert.IsType<DefaultRazorTagHelperBinderPhase>(phase),
                phase => Assert.IsType<DefaultRazorIntermediateNodeLoweringPhase>(phase),
                phase => Assert.IsType<DefaultRazorDocumentClassifierPhase>(phase),
                phase => Assert.IsType<DefaultRazorDirectiveClassifierPhase>(phase),
                phase => Assert.IsType<DefaultRazorOptimizationPhase>(phase),
                phase => Assert.IsType<DefaultRazorCSharpLoweringPhase>(phase));
        }

        private static void AssertDefaultFeatures(RazorProjectEngine engine)
        {
            var features = engine.EngineFeatures.OrderBy(f => f.GetType().Name).ToArray();
            Assert.Collection(
                features,
                feature => Assert.IsType<DefaultDirectiveSyntaxTreePass>(feature),
                feature => Assert.IsType<DefaultDocumentClassifierPass>(feature),
                feature => Assert.IsType<DefaultDocumentClassifierPassFeature>(feature),
                feature => Assert.IsType<DefaultMetadataIdentifierFeature>(feature),
                feature => Assert.IsType<DefaultRazorCodeGenerationOptionsFeature>(feature),
                feature => Assert.IsType<DefaultRazorDirectiveFeature>(feature),
                feature => Assert.IsType<DefaultRazorParserOptionsFeature>(feature),
                feature => Assert.IsType<DefaultRazorTargetExtensionFeature>(feature),
                feature => Assert.IsType<DefaultTagHelperOptimizationPass>(feature),
                feature => Assert.IsType<DesignTimeDirectivePass>(feature),
                feature => Assert.IsType<DirectiveRemovalOptimizationPass>(feature),
                feature => Assert.IsType<HtmlNodeOptimizationPass>(feature),
                feature => Assert.IsType<MetadataAttributePass>(feature),
                feature => Assert.IsType<PreallocatedTagHelperAttributeOptimizationPass>(feature));
        }

        private static void AssertDefaultDirectives(RazorProjectEngine engine)
        {
            var feature = engine.EngineFeatures.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            Assert.NotNull(feature);
            Assert.Empty(feature.Directives);
        }

        private static void AssertDefaultTargetExtensions(RazorProjectEngine engine)
        {
            var feature = engine.EngineFeatures.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
            Assert.NotNull(feature);

            var extensions = feature.TargetExtensions.OrderBy(f => f.GetType().Name).ToArray();
            Assert.Collection(
                extensions,
                extension => Assert.IsType<DefaultTagHelperTargetExtension>(extension),
                extension => Assert.IsType<DesignTimeDirectiveTargetExtension>(extension),
                extension => Assert.IsType<MetadataAttributeTargetExtension>(extension),
                extension => Assert.IsType<PreallocatedAttributeTargetExtension>(extension));
        }
    }
}
