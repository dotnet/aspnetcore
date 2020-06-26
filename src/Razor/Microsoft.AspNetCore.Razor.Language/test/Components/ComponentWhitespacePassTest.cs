// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    public class ComponentWhitespacePassTest
    {
        public ComponentWhitespacePassTest()
        {
            Pass = new ComponentWhitespacePass();
            ProjectEngine = (DefaultRazorProjectEngine)RazorProjectEngine.Create(
                RazorConfiguration.Default,
                RazorProjectFileSystem.Create(Environment.CurrentDirectory),
                b =>
                {
                    if (b.Features.OfType<ComponentWhitespacePass>().Any())
                    {
                        b.Features.Remove(b.Features.OfType<ComponentWhitespacePass>().Single());
                    }
                });
            Engine = ProjectEngine.Engine;

            Pass.Engine = Engine;
        }

        private DefaultRazorProjectEngine ProjectEngine { get; }

        private RazorEngine Engine { get; }

        private ComponentWhitespacePass Pass { get; }

        [Fact]
        public void Execute_RemovesLeadingAndTrailingWhitespace()
        {
            // Arrange
            var document = CreateDocument(@"

<span>@(""Hello, world"")</span>

");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var method = documentNode.FindPrimaryMethod();
            var child = (MarkupElementIntermediateNode)method.Children.Single();
            Assert.Equal("span", child.TagName);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            var source = RazorSourceDocument.Create(content, "test.cshtml");
            return ProjectEngine.CreateCodeDocumentCore(source, FileKinds.Component);
        }

        private DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < Engine.Phases.Count; i++)
            {
                var phase = Engine.Phases[i];
                if (phase is IRazorCSharpLoweringPhase)
                {
                    break;
                }

                phase.Execute(codeDocument);
            }

            return codeDocument.GetDocumentIntermediateNode();
        }
    }
}
