// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeNodeWriterTest
    {
        [Fact]
        public void WriteUsingDirective_NoSource_WritesContent()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new UsingDirectiveIntermediateNode()
            {
                Content = "System",
            };

            // Act
            writer.WriteUsingDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"using System;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteUsingDirective_WithSource_WritesContentWithLinePragmaAndMapping()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var sourceDocument = TestRazorSourceDocument.Create("@using System;");
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument, options);

            var originalSpan = new SourceSpan("test.cshtml", 0, 0, 0, 6);
            var generatedSpan = new SourceSpan(null, 21 + Environment.NewLine.Length, 1, 0, 6);
            var expectedLineMapping = new LineMapping(originalSpan, generatedSpan);
            var node = new UsingDirectiveIntermediateNode()
            {
                Content = "System",
                Source = originalSpan,
            };

            // Act
            writer.WriteUsingDirective(context, node);

            // Assert
            var mapping = Assert.Single(context.GetLineMappings());
            Assert.Equal(expectedLineMapping, mapping);
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
using System;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_SkipsLinePragma_WithoutSource()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpExpressionIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i++",
                Kind = IntermediateToken.TokenKind.CSharp,
            });

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__o = i++;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_WritesLinePragma_WithSource()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpExpressionIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i++",
                Kind = IntermediateToken.TokenKind.CSharp,
            });

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
__o = i++;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_WithExtensionNode_WritesPadding()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpExpressionIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i",
                Kind = IntermediateToken.TokenKind.CSharp,
            });
            builder.Add(new MyExtensionIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "++",
                Kind = IntermediateToken.TokenKind.CSharp,
            });

            context.SetRenderNode((n) => Assert.IsType<MyExtensionIntermediateNode>(n));

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"__o = i++;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_WithSource_WritesPadding()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var sourceDocument = TestRazorSourceDocument.Create("       @i++");
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument, options);

            var node = new CSharpExpressionIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 8, 0, 8, 3),
            };
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i",
                Kind = IntermediateToken.TokenKind.CSharp,
            });
            builder.Add(new MyExtensionIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "++",
                Kind = IntermediateToken.TokenKind.CSharp,
            });

            context.SetRenderNode((n) => Assert.IsType<MyExtensionIntermediateNode>(n));

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
  __o = i++;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCode_WhitespaceContent_DoesNothing()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpCodeIntermediateNode();
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = IntermediateToken.TokenKind.CSharp,
                    Content = "  \t"
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Empty(csharp);
        }

        [Fact]
        public void WriteCSharpCode_WhitespaceContentWithSource_WritesContent()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpCodeIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = IntermediateToken.TokenKind.CSharp,
                    Content = "    "
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"    
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCode_SkipsLinePragma_WithoutSource()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpCodeIntermediateNode();
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = IntermediateToken.TokenKind.CSharp,
                    Content = "if (true) { }"
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"if (true) { }
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCode_WritesLinePragma_WithSource()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpCodeIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 13),
            };
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = IntermediateToken.TokenKind.CSharp,
                    Content = "if (true) { }",
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
if (true) { }

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCode_WritesPadding_WithSource()
        {
            // Arrange
            var writer = new DesignTimeNodeWriter();
            var codeWriter = new CodeWriter();
            var options = RazorCodeGenerationOptions.CreateDefault();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument: null, options: options);

            var node = new CSharpCodeIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 17),
            };
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = IntermediateToken.TokenKind.CSharp,
                    Content = "    if (true) { }",
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
    if (true) { }

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpressionAttributeValue_RendersCorrectly()
        {
            var writer = new DesignTimeNodeWriter();

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var context = GetCodeRenderingContext(writer, sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpExpressionAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpExpressionAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
                       __o = false;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCodeAttributeValue_RendersCorrectly()
        {
            var writer = new DesignTimeNodeWriter();
            var content = "<input checked=\"hello-world @if(@true){ }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var context = GetCodeRenderingContext(writer, sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpCodeAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpCodeAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
                             if(@true){ }

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCodeAttributeValue_WithExpression_RendersCorrectly()
        {
            var writer = new DesignTimeNodeWriter();
            var content = "<input checked=\"hello-world @if(@true){ @false }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var context = GetCodeRenderingContext(writer, sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpCodeAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpCodeAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
                             if(@true){ 

#line default
#line hidden
Render Node - CSharpExpressionIntermediateNode
#line 1 ""test.cshtml""
                                               }

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private static CodeRenderingContext GetCodeRenderingContext(IntermediateNodeWriter writer, RazorSourceDocument sourceDocument)
        {
            var options = RazorCodeGenerationOptions.CreateDefault();
            var codeWriter = new CodeWriter();
            var context = new DefaultCodeRenderingContext(codeWriter, writer, sourceDocument, options);
            context.SetRenderNode(n =>
            {
                codeWriter.WriteLine($"Render Node - {n.GetType().Name}");
            });

            return context;
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            return Lower(codeDocument, engine);
        }

        private static DocumentIntermediateNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIntermediateNodeLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetDocumentIntermediateNode();
            Assert.NotNull(irDocument);

            return irDocument;
        }

        private class MyExtensionIntermediateNode : ExtensionIntermediateNode
        {
            public override IntermediateNodeCollection Children => ReadOnlyIntermediateNodeCollection.Instance;

            public override void Accept(IntermediateNodeVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public override void WriteNode(CodeTarget target, CodeRenderingContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
