// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;
using ErrorSink = Microsoft.AspNetCore.Razor.Evolution.Legacy.ErrorSink;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "p",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = "System.Int32",
                            Name = "Foo",
                        }

                    }
                }
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

            var child = Assert.IsType<HtmlContentIRNode>(Assert.Single(setProperty.Children));
            Assert.Equal("17", child.Content);
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "p",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = typeof(ModelExpression).FullName,
                            Name = "Foo",
                        }

                    }
                }
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

            var originalNode = Assert.IsType<CSharpTokenIRNode>(expression.Children[2]);
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
                new TagHelperDescriptor()
                {
                    AssemblyName = "TestAssembly",
                    TypeName = "TestTagHelper",
                    TagName = "p",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor()
                        {
                            TypeName = typeof(ModelExpression).FullName,
                            Name = "Foo",
                        }

                    }
                }
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

            var originalNode = Assert.IsType<CSharpTokenIRNode>(expression.Children[1]);
            Assert.Equal("Bar", originalNode.Content);
            Assert.Equal(new SourceSpan("test.cshtml", 52, 1, 9, 3), originalNode.Source.Value);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            using (var stream = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0L, SeekOrigin.Begin);

                var source = RazorSourceDocument.ReadFrom(stream, "test.cshtml");
                return RazorCodeDocument.Create(source);
            }
        }

        private RazorEngine CreateEngine(params TagHelperDescriptor[] tagHelpers)
        {
            return RazorEngine.Create(b =>
            {
                b.Features.Add(new TagHelperFeature(tagHelpers));
            });
        }

        private DocumentIRNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRPhase)
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
                var child = node.Children[i] as CSharpTokenIRNode;
                builder.Append(child.Content);
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

        private class TagHelperFeature : ITagHelperFeature
        {
            public TagHelperFeature(TagHelperDescriptor[] tagHelpers)
            {
                Resolver = new TagHelperDescriptorResolver(tagHelpers);
            }

            public RazorEngine Engine { get; set; }

            public ITagHelperDescriptorResolver Resolver { get; }
        }

        private class TagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            public TagHelperDescriptorResolver(TagHelperDescriptor[] tagHelpers)
            {
                TagHelpers = tagHelpers;
            }

            public TagHelperDescriptor[] TagHelpers { get; }

            public IEnumerable<TagHelperDescriptor> Resolve(ErrorSink errorSink)
            {
                return TagHelpers;
            }
        }
    }
}