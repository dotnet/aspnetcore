// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorParsingPhaseTest
    {
        [Fact]
        public void Execute_AddsSyntaxTree()
        {
            // Arrange
            var phase = new DefaultRazorParsingPhase();
            var engine = RazorEngine.CreateEmpty(b => b.Phases.Add(phase));

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            phase.Execute(codeDocument);

            // Assert
            Assert.NotNull(codeDocument.GetSyntaxTree());
        }

        [Fact]
        public void Execute_UsesConfigureParserFeatures()
        {
            // Arrange
            var phase = new DefaultRazorParsingPhase();
            var engine = RazorEngine.CreateEmpty((b) =>
            {
                b.Phases.Add(phase);
                b.Features.Add(new MyParserOptionsFeature());
            });

            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            phase.Execute(codeDocument);

            // Assert
            var syntaxTree = codeDocument.GetSyntaxTree();
            var directive = Assert.Single(syntaxTree.Options.Directives);
            Assert.Equal("test", directive.Directive);
        }

        [Fact]
        public void Execute_ParsesImports()
        {
            // Arrange
            var phase = new DefaultRazorParsingPhase();
            var engine = RazorEngine.CreateEmpty((b) =>
            {
                b.Phases.Add(phase);
                b.Features.Add(new MyParserOptionsFeature());
            });

            var imports = new[]
            {
                TestRazorSourceDocument.Create(),
                TestRazorSourceDocument.Create(),
            };

            var codeDocument = TestRazorCodeDocument.Create(TestRazorSourceDocument.Create(), imports);

            // Act
            phase.Execute(codeDocument);

            // Assert
            Assert.Collection(
                codeDocument.GetImportSyntaxTrees(),
                t => { Assert.Same(t.Source, imports[0]); Assert.Equal("test", Assert.Single(t.Options.Directives).Directive); },
                t => { Assert.Same(t.Source, imports[1]); Assert.Equal("test", Assert.Single(t.Options.Directives).Directive); });
        }

        private class MyParserOptionsFeature : RazorEngineFeatureBase, IRazorParserOptionsFeature
        {
            public int Order { get; }

            public void Configure(RazorParserOptionsBuilder options)
            {
                options.Directives.Add(DirectiveDescriptor.CreateDirective("test", DirectiveKind.SingleLine));
            }
        }
    }
}
