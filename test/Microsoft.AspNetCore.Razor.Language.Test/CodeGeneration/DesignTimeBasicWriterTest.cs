// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeBasicWriterTest
    {
        [Fact]
        public void WriteUsingStatement_NoSource_WritesContent()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter()
            };

            var node = new UsingStatementIntermediateNode()
            {
                Content = "System",
            };

            // Act
            writer.WriteUsingStatement(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"using System;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteUsingStatement_WithSource_WritesContentWithLinePragmaAndMapping()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();
            var sourceDocument = TestRazorSourceDocument.Create("@using System;");
            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(sourceDocument)
            };

            var originalSpan = new SourceSpan("test.cshtml", 0, 0, 0, 6);
            var generatedSpan = new SourceSpan(null, 21 + Environment.NewLine.Length, 1, 0, 6);
            var expectedLineMapping = new LineMapping(originalSpan, generatedSpan);
            var node = new UsingStatementIntermediateNode()
            {
                Content = "System",
                Source = originalSpan,
            };

            // Act
            writer.WriteUsingStatement(context, node);

            // Assert
            var mapping = Assert.Single(context.LineMappings);
            Assert.Equal(expectedLineMapping, mapping);
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
            };

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
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
                Writer = new CSharpCodeWriter(),
            };

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
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
            };

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

            context.RenderNode = (n) => Assert.IsType<MyExtensionIntermediateNode>(n);

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();
            var sourceDocument = TestRazorSourceDocument.Create("       @i++");

            var context = new CSharpRenderingContext()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
                CodeDocument = RazorCodeDocument.Create(sourceDocument),
                Writer = new CSharpCodeWriter(),
            };

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

            context.RenderNode = (n) => Assert.IsType<MyExtensionIntermediateNode>(n);

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
            };

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
            var csharp = context.Writer.Builder.ToString();
            Assert.Empty(csharp);
        }

        [Fact]
        public void WriteCSharpCode_WhitespaceContentWithSource_WritesContent()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

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
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
            };

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
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

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
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

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
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            context.CodeDocument = codeDocument;
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpExpressionAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpExpressionAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @if(@true){ }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            context.CodeDocument = codeDocument;
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpCodeAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpCodeAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
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
            var writer = new DesignTimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @if(@true){ @false }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            context.CodeDocument = codeDocument;
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpCodeAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpCodeAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
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

        private static CSharpRenderingContext GetCSharpRenderingContext(BasicWriter writer)
        {
            var options = RazorCodeGenerationOptions.CreateDefault();
            var codeWriter = new CSharpCodeWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = codeWriter,
                Options = options,
                BasicWriter = writer,
                RenderChildren = n =>
                {
                    codeWriter.WriteLine("Render Children");
                },
                RenderNode = n =>
                {
                    codeWriter.WriteLine($"Render Node - {n.GetType().Name}");
                }
            };

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

            public override void WriteNode(CodeTarget target, CSharpRenderingContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
