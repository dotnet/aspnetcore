// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
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
                feature => Assert.IsType<AttributeDirectivePass>(feature),
                feature => Assert.IsType<ComponentBindLoweringPass>(feature),
                feature => Assert.IsType<ComponentChildContentDiagnosticPass>(feature),
                feature => Assert.IsType<ComponentComplexAttributeContentPass>(feature),
                feature => Assert.IsType<ComponentCssScopePass>(feature),
                feature => Assert.IsType<ComponentDocumentClassifierPass>(feature),
                feature => Assert.IsType<ComponentEventHandlerLoweringPass>(feature),
                feature => Assert.IsType<ComponentGenericTypePass>(feature),
                feature => Assert.IsType<ComponentInjectDirectivePass>(feature),
                feature => Assert.IsType<ComponentKeyLoweringPass>(feature),
                feature => Assert.IsType<ComponentLayoutDirectivePass>(feature),
                feature => Assert.IsType<ComponentLoweringPass>(feature),
                feature => Assert.IsType<ComponentMarkupBlockPass>(feature),
                feature => Assert.IsType<ComponentMarkupDiagnosticPass>(feature),
                feature => Assert.IsType<ComponentMarkupEncodingPass>(feature),
                feature => Assert.IsType<ComponentPageDirectivePass>(feature),
                feature => Assert.IsType<ComponentReferenceCaptureLoweringPass>(feature),
                feature => Assert.IsType<ComponentScriptTagPass>(feature),
                feature => Assert.IsType<ComponentSplatLoweringPass>(feature),
                feature => Assert.IsType<ComponentTemplateDiagnosticPass>(feature),
                feature => Assert.IsType<ComponentWhitespacePass>(feature),
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
                feature => Assert.IsType<EliminateMethodBodyPass>(feature),
                feature => Assert.IsType<FunctionsDirectivePass>(feature),
                feature => Assert.IsType<HtmlNodeOptimizationPass>(feature),
                feature => Assert.IsType<ImplementsDirectivePass>(feature),
                feature => Assert.IsType<InheritsDirectivePass>(feature),
                feature => Assert.IsType<MetadataAttributePass>(feature),
                feature => Assert.IsType<PreallocatedTagHelperAttributeOptimizationPass>(feature),
                feature => Assert.IsType<ViewCssScopePass>(feature));
        }

        private static void AssertDefaultDirectives(RazorProjectEngine engine)
        {
            var feature = engine.EngineFeatures.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            Assert.NotNull(feature);
            Assert.Collection(
                feature.Directives,
                directive => Assert.Same(FunctionsDirective.Directive, directive),
                directive => Assert.Same(ImplementsDirective.Directive, directive),
                directive => Assert.Same(InheritsDirective.Directive, directive),
                directive => Assert.Same(NamespaceDirective.Directive, directive),
                directive => Assert.Same(AttributeDirective.Directive, directive));
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
