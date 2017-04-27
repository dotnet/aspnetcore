// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ModelExpressionPassTest
    {
        [Fact]
        public void ModelExpressionPass_NonModelExpressionProperty_Ignored()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<p foo=""17"">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                    .BindAttribute(attribute =>
                        attribute
                            .Name("Foo")
                            .TypeName("System.Int32"))
                    .TagMatchingRule(rule =>
                        rule.RequireTagName("p"))
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ModelExpressionPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            var setProperty = tagHelper.Children.OfType<SetTagHelperPropertyIRNode>().Single();

            var html = Assert.IsType<HtmlContentIRNode>(Assert.Single(setProperty.Children));
            var token = Assert.IsType<RazorIRToken>(Assert.Single(html.Children));
            Assert.Equal("17", token.Content);
        }

        [Fact]
        public void ModelExpressionPass_ModelExpressionProperty_SimpleExpression()
        {
            // Arrange

            // Using \r\n here because we verify line mappings
            var codeDocument = CreateDocument(
                "@addTagHelper TestTagHelper, TestAssembly\r\n<p foo=\"Bar\">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                    .BindAttribute(attribute =>
                        attribute
                            .Name("Foo")
                            .TypeName(typeof(ModelExpression).FullName))
                    .TagMatchingRule(rule =>
                        rule.RequireTagName("p"))
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ModelExpressionPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            var setProperty = tagHelper.Children.OfType<SetTagHelperPropertyIRNode>().Single();

            var expression = Assert.IsType<CSharpExpressionIRNode>(Assert.Single(setProperty.Children));
            Assert.Equal("ModelExpressionProvider.CreateModelExpression(ViewData, __model => __model.Bar)", GetCSharpContent(expression));

            var originalNode = Assert.IsType<RazorIRToken>(expression.Children[2]);
            Assert.Equal(RazorIRToken.TokenKind.CSharp, originalNode.Kind);
            Assert.Equal("Bar", originalNode.Content);
            Assert.Equal(new SourceSpan("test.cshtml", 51, 1, 8, 3), originalNode.Source.Value);
        }

        [Fact]
        public void ModelExpressionPass_ModelExpressionProperty_ComplexExpression()
        {
            // Arrange

            // Using \r\n here because we verify line mappings
            var codeDocument = CreateDocument(
                "@addTagHelper TestTagHelper, TestAssembly\r\n<p foo=\"@Bar\">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                    .BindAttribute(attribute =>
                        attribute
                            .Name("Foo")
                            .TypeName(typeof(ModelExpression).FullName))
                    .TagMatchingRule(rule =>
                        rule.RequireTagName("p"))
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new ModelExpressionPass()
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var tagHelper = FindTagHelperNode(irDocument);
            var setProperty = tagHelper.Children.OfType<SetTagHelperPropertyIRNode>().Single();

            var expression = Assert.IsType<CSharpExpressionIRNode>(Assert.Single(setProperty.Children));
            Assert.Equal("ModelExpressionProvider.CreateModelExpression(ViewData, __model => Bar)", GetCSharpContent(expression));

            var originalNode = Assert.IsType<RazorIRToken>(expression.Children[1]);
            Assert.Equal(RazorIRToken.TokenKind.CSharp, originalNode.Kind);
            Assert.Equal("Bar", originalNode.Content);
            Assert.Equal(new SourceSpan("test.cshtml", 52, 1, 9, 3), originalNode.Source.Value);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            var source = RazorSourceDocument.Create(content, "test.cshtml");
            return RazorCodeDocument.Create(source);
        }

        private RazorEngine CreateEngine(params TagHelperDescriptor[] tagHelpers)
        {
            return RazorEngine.Create(b =>
            {
                b.Features.Add(new TestTagHelperFeature(tagHelpers));
            });
        }

        private DocumentIRNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDirectiveClassifierPhase)
                {
                    break;
                }
            }

            return codeDocument.GetIRDocument();
        }

        private TagHelperIRNode FindTagHelperNode(RazorIRNode node)
        {
            var visitor = new TagHelperNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private string GetCSharpContent(RazorIRNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i] as RazorIRToken;
                if (child.Kind == RazorIRToken.TokenKind.CSharp)
                {
                    builder.Append(child.Content);
                }
            }

            return builder.ToString();
        }

        private class TagHelperNodeVisitor : RazorIRNodeWalker
        {
            public TagHelperIRNode Node { get; set; }

            public override void VisitTagHelper(TagHelperIRNode node)
            {
                Node = node;
            }
        }
    }
}