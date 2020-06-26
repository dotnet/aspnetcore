// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
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
            var child = Assert.IsType<MarkupElementIntermediateNode>(Assert.Single(method.Children));
            Assert.Equal("span", child.TagName);
        }

        [Fact]
        public void Execute_RemovesLeadingAndTrailingWhitespaceInsideElement()
        {
            // Arrange
            var document = CreateDocument(@"
<parent>
    <child>   Hello, @("" w o r l d "")   </child>
</parent>
");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            var parentElement = Assert.IsType<MarkupElementIntermediateNode>(Assert.Single(documentNode.FindPrimaryMethod().Children));
            var childElement = Assert.IsType<MarkupElementIntermediateNode>(Assert.Single(parentElement.Children));
            Assert.Equal("child", childElement.TagName);
            Assert.Collection(childElement.Children,
                node =>
                {
                    var htmlNode = Assert.IsType<HtmlContentIntermediateNode>(node);
                    Assert.Equal("   Hello, ", GetContent(htmlNode));
                },
                node =>
                {
                    var csharpExpressionNode = Assert.IsType<CSharpExpressionIntermediateNode>(node);
                    Assert.Equal(@""" w o r l d """, GetContent(csharpExpressionNode));
                });
        }

        [Fact]
        public void Execute_LeavesWhitespaceBetweenSiblingElements()
        {
            // Arrange
            var document = CreateDocument(@" <elem attr=@expr /> <elem attr=@expr /> ");

            var documentNode = Lower(document);

            // Act
            Pass.Execute(document, documentNode);

            // Assert
            Assert.Collection(documentNode.FindPrimaryMethod().Children,
                node => Assert.IsType<MarkupElementIntermediateNode>(node),
                node => Assert.IsType<HtmlContentIntermediateNode>(node),
                node => Assert.IsType<MarkupElementIntermediateNode>(node));
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

        private static string GetContent(IntermediateNode node)
        {
            var builder = new StringBuilder();
            var tokens = node.Children.OfType<IntermediateToken>();
            foreach (var token in tokens)
            {
                builder.Append(token.Content);
            }
            return builder.ToString();
        }
    }
}
