// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeNodeWriterTest
    {
        [Fact]
        public void WriteUsingDirective_NoSource_WritesContent()
        {
            // Arrange
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new UsingDirectiveIntermediateNode()
            {
                Content = "System",
            };

            // Act
            writer.WriteUsingDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"using System;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteUsingDirective_WithSource_WritesContentWithLinePragma()
        {
            // Arrange
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new UsingDirectiveIntermediateNode()
            {
                Content = "System",
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };

            // Act
            writer.WriteUsingDirective(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpExpressionIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i++",
                Kind = TokenKind.CSharp,
            });

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpExpressionIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i++",
                Kind = TokenKind.CSharp,
            });

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpExpressionIntermediateNode();
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i",
                Kind = TokenKind.CSharp,
            });
            builder.Add(new MyExtensionIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "++",
                Kind = TokenKind.CSharp,
            });

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"Test(iRender Children
++);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_WithSource_WritesPadding()
        {
            // Arrange
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter()
            {
                WriteCSharpExpressionMethod = "Test",
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpExpressionIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 8, 0, 8, 3),
            };
            var builder = IntermediateNodeBuilder.Create(node);
            builder.Add(new IntermediateToken()
            {
                Content = "i",
                Kind = TokenKind.CSharp,
            });
            builder.Add(new MyExtensionIntermediateNode());
            builder.Add(new IntermediateToken()
            {
                Content = "++",
                Kind = TokenKind.CSharp,
            });

            // Act
            writer.WriteCSharpExpression(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"#line 1 ""test.cshtml""
   Test(iRender Children
++);

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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpCodeIntermediateNode();
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = TokenKind.CSharp,
                    Content = "  \t"
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Empty(csharp);
        }

        [Fact]
        public void WriteCSharpCode_SkipsLinePragma_WithoutSource()
        {
            // Arrange
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpCodeIntermediateNode();
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = TokenKind.CSharp,
                    Content = "if (true) { }"
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpCodeIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 13),
            };
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = TokenKind.CSharp,
                    Content = "if (true) { }",
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new CSharpCodeIntermediateNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 17),
            };
            IntermediateNodeBuilder.Create(node)
                .Add(new IntermediateToken()
                {
                    Kind = TokenKind.CSharp,
                    Content = "    if (true) { }",
                });

            // Act
            writer.WriteCSharpCode(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new HtmlContentIntermediateNode();
            node.Children.Add(new IntermediateToken()
            {
                Content = "SomeContent",
                Kind = TokenKind.Html,
            });

            // Act
            writer.WriteHtmlContent(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var codeWriter = new CodeWriter();
            var writer = new RuntimeNodeWriter();
            var context = TestCodeRenderingContext.CreateRuntime();

            var node = new HtmlContentIntermediateNode();
            node.Children.Add(new IntermediateToken()
            {
                Content = new string('*', 2000),
                Kind = TokenKind.Html,
            });

            // Act
            writer.WriteHtmlContent(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var writer = new RuntimeNodeWriter();
            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single();

            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.WriteHtmlAttribute(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"BeginWriteAttribute(""checked"", "" checked=\"""", 6, ""\"""", 34, 2);
Render Children
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
            var writer = new RuntimeNodeWriter();
            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[0] as HtmlAttributeValueIntermediateNode;
            
            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.WriteHtmlAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var writer = new RuntimeNodeWriter();
            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpExpressionAttributeValueIntermediateNode;

            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.WriteCSharpExpressionAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var writer = new RuntimeNodeWriter();

            var content = "<input checked=\"hello-world @if(@true){ }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var documentNode = Lower(codeDocument);
            var node = documentNode.Children.OfType<HtmlAttributeIntermediateNode>().Single().Children[1] as CSharpCodeAttributeValueIntermediateNode;

            var context = TestCodeRenderingContext.CreateRuntime(source: sourceDocument);

            // Act
            writer.WriteCSharpCodeAttributeValue(context, node);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var writer = new RuntimeNodeWriter()
            {
                PushWriterMethod = "TestPushWriter"
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.BeginWriterScope(context, "MyWriter");

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
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
            var writer = new RuntimeNodeWriter()
            {
                PopWriterMethod = "TestPopWriter"
            };
            var context = TestCodeRenderingContext.CreateRuntime();

            // Act
            writer.EndWriterScope(context);

            // Assert
            var csharp = context.CodeWriter.GenerateCode();
            Assert.Equal(
@"TestPopWriter();
",
                csharp,
                ignoreLineEndingDifferences: true);
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
            public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

            public override void Accept(IntermediateNodeVisitor visitor)
            {
                visitor.VisitDefault(this);
            }

            public override void WriteNode(CodeTarget target, CodeRenderingContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
