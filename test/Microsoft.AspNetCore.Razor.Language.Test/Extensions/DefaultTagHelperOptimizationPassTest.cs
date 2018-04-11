// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public class DefaultTagHelperOptimizationPassTest
    {
        [Fact]
        public void DefaultTagHelperOptimizationPass_Execute_ReplacesChildren()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@addTagHelper TestTagHelper, TestAssembly
<p foo=""17"" attr=""value"">");

            var tagHelpers = new[]
            {
                TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                    .TypeName("TestTagHelper")
                    .BoundAttributeDescriptor(attribute => attribute
                        .Name("Foo")
                        .TypeName("System.Int32")
                        .PropertyName("FooProp"))
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .Build()
            };

            var engine = CreateEngine(tagHelpers);
            var pass = new DefaultTagHelperOptimizationPass()
            {
                Engine = engine
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = irDocument.FindPrimaryClass();
            Assert.IsType<DefaultTagHelperRuntimeIntermediateNode>(@class.Children[0]);

            var fieldDeclaration = Assert.IsType<FieldDeclarationIntermediateNode>(@class.Children[1]);
            Assert.Equal(bool.TrueString, fieldDeclaration.Annotations[CommonAnnotations.DefaultTagHelperExtension.TagHelperField]);
            Assert.Equal("__TestTagHelper", fieldDeclaration.FieldName);
            Assert.Equal("global::TestTagHelper", fieldDeclaration.FieldType);
            Assert.Equal("private", fieldDeclaration.Modifiers.First());

            var tagHelper = FindTagHelperNode(irDocument);
            Assert.Equal(5, tagHelper.Children.Count);

            var body = Assert.IsType<DefaultTagHelperBodyIntermediateNode>(tagHelper.Children[0]);
            Assert.Equal("p", body.TagName);
            Assert.Equal(TagMode.StartTagAndEndTag, body.TagMode);

            var create = Assert.IsType<DefaultTagHelperCreateIntermediateNode>(tagHelper.Children[1]);
            Assert.Equal("__TestTagHelper", create.FieldName);
            Assert.Equal("TestTagHelper", create.TypeName);
            Assert.Equal(tagHelpers[0], create.TagHelper, TagHelperDescriptorComparer.CaseSensitive);

            var property = Assert.IsType<DefaultTagHelperPropertyIntermediateNode>(tagHelper.Children[2]);
            Assert.Equal("foo", property.AttributeName);
            Assert.Equal(AttributeStructure.DoubleQuotes, property.AttributeStructure);
            Assert.Equal(tagHelpers[0].BoundAttributes[0], property.BoundAttribute, BoundAttributeDescriptorComparer.CaseSensitive);
            Assert.Equal("__TestTagHelper", property.FieldName);
            Assert.False(property.IsIndexerNameMatch);
            Assert.Equal("FooProp", property.PropertyName);
            Assert.Equal(tagHelpers[0], property.TagHelper, TagHelperDescriptorComparer.CaseSensitive);

            var htmlAttribute = Assert.IsType<DefaultTagHelperHtmlAttributeIntermediateNode>(tagHelper.Children[3]);
            Assert.Equal("attr", htmlAttribute.AttributeName);
            Assert.Equal(AttributeStructure.DoubleQuotes, htmlAttribute.AttributeStructure);

            Assert.IsType<DefaultTagHelperExecuteIntermediateNode>(tagHelper.Children[4]);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            var source = RazorSourceDocument.Create(content, "test.cshtml");
            return RazorCodeDocument.Create(source);
        }

        private RazorEngine CreateEngine(params TagHelperDescriptor[] tagHelpers)
        {
            return RazorProjectEngine.Create(b =>
            {
                b.Features.Add(new TestTagHelperFeature(tagHelpers));
            }).Engine;
        }

        private DocumentIntermediateNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
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

            return codeDocument.GetDocumentIntermediateNode();
        }

        private TagHelperIntermediateNode FindTagHelperNode(IntermediateNode node)
        {
            var visitor = new TagHelperNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private class TagHelperNodeVisitor : IntermediateNodeWalker
        {
            public TagHelperIntermediateNode Node { get; set; }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                Node = node;
            }
        }
    }
}
