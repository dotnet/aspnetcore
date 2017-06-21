// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeBasicWriterTest
    {
        [Fact]
        public void WriteUsingStatement_NoSource_WritesContent()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter()
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
        public void WriteUsingStatement_WithSource_WritesContentWithLinePragma()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter()
            };

            var node = new UsingStatementIntermediateNode()
            {
                Content = "System",
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };

            // Act
            writer.WriteUsingStatement(context, node);

            // Assert
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
            var writer = new RuntimeBasicWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
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
@"Test(i++);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_WritesLinePragma_WithSource()
        {
            // Arrange
            var writer = new RuntimeBasicWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
                Writer = new Legacy.CSharpCodeWriter(),
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
Test(i++);

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
            var writer = new RuntimeBasicWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
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
@"Test(i++);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_WithSource_WritesPadding()
        {
            // Arrange
            var writer = new RuntimeBasicWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };
            var sourceDocument = TestRazorSourceDocument.Create("       @i++");

            var context = new CSharpRenderingContext()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
                CodeDocument = RazorCodeDocument.Create(sourceDocument),
                Writer = new Legacy.CSharpCodeWriter(),
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
   Test(i++);

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
            var writer = new RuntimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
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
        public void WriteCSharpCode_SkipsLinePragma_WithoutSource()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
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
            var writer = new RuntimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
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
            var writer = new RuntimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
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
        public void WriteHtmlContent_RendersContentCorrectly()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var node = new HtmlContentIntermediateNode();
            node.Children.Add(new IntermediateToken()
            {
                Content = "SomeContent",
                Kind = IntermediateToken.TokenKind.Html,
            });

            // Act
            writer.WriteHtmlContent(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"WriteLiteral(""SomeContent"");
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteHtmlContent_LargeStringLiteral_UsesMultipleWrites()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };


            var node = new HtmlContentIntermediateNode();
            node.Children.Add(new IntermediateToken()
            {
                Content = new string('*', 2000),
                Kind = IntermediateToken.TokenKind.Html,
            });

            // Act
            writer.WriteHtmlContent(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(string.Format(
@"WriteLiteral(@""{0}"");
WriteLiteral(@""{1}"");
", new string('*', 1024), new string('*', 976)),
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteHtmlAttribute_RendersCorrectly()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single();

            // Act
            writer.WriteHtmlAttribute(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"BeginWriteAttribute(""checked"", "" checked=\"""", 6, ""\"""", 34, 2);
Render Children
EndWriteAttribute();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteHtmlAttributeValue_RendersCorrectly()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[0] as HtmlAttributeValueIntermediateNode;

            // Act
            writer.WriteHtmlAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"WriteAttributeValue("""", 16, ""hello-world"", 16, 11, true);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpressionAttributeValue_RendersCorrectly()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpExpressionAttributeValueIntermediateNode;

            // Act
            writer.WriteCSharpExpressionAttributeValue(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#line 1 ""test.cshtml""
WriteAttributeValue("" "", 27, false, 28, 6, false);

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpCodeAttributeValue_BuffersResult()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
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
@"WriteAttributeValue("" "", 27, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
    PushWriter(__razor_attribute_value_writer);
#line 1 ""test.cshtml""
                             if(@true){ }

#line default
#line hidden
    PopWriter();
}
), 28, 13, false);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void BeginWriterScope_UsesSpecifiedWriter_RendersCorrectly()
        {
            // Arrange
            var writer = new RuntimeBasicWriter()
            {
                PushWriterMethod = "TestPushWriter"
            };
            var context = GetCSharpRenderingContext(writer);

            // Act
            writer.BeginWriterScope(context, "MyWriter");

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"TestPushWriter(MyWriter);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void EndWriterScope_RendersCorrectly()
        {
            // Arrange
            var writer = new RuntimeBasicWriter()
            {
                PopWriterMethod = "TestPopWriter"
            };
            var context = GetCSharpRenderingContext(writer);

            // Act
            writer.EndWriterScope(context);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"TestPopWriter();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private static CSharpRenderingContext GetCSharpRenderingContext(BasicWriter writer)
        {
            var options = RazorCodeGenerationOptions.CreateDefault();
            var codeWriter = new Legacy.CSharpCodeWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = codeWriter,
                Options = options,
                BasicWriter = writer,
                RenderChildren = n =>
                {
                    codeWriter.WriteLine("Render Children");
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

            public override SourceSpan? Source { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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
