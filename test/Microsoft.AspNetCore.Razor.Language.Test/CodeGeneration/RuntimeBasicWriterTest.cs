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
        public void WriteChecksum_WritesPragmaChecksum()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter()
            };

            var node = new ChecksumIRNode()
            {
                FileName = "test.cshtml",
                Guid = "SomeGuid",
                Bytes = "SomeFileHash"
            };

            // Act
            writer.WriteChecksum(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma checksum ""test.cshtml"" ""SomeGuid"" ""SomeFileHash""
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteChecksum_EmptyBytes_WritesNothing()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter()
            };

            var node = new ChecksumIRNode()
            {
                FileName = "test.cshtml",
                Guid = "SomeGuid",
                Bytes = string.Empty
            };

            // Act
            writer.WriteChecksum(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Empty(csharp);
        }

        [Fact]
        public void WriteUsingStatement_NoSource_WritesContent()
        {
            // Arrange
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter()
            };

            var node = new UsingStatementIRNode()
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

            var node = new UsingStatementIRNode()
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

            var node = new CSharpExpressionIRNode();
            var builder = RazorIRBuilder.Create(node);
            builder.Add(new RazorIRToken()
            {
                Content = "i++",
                Kind = RazorIRToken.TokenKind.CSharp,
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

            var node = new CSharpExpressionIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };
            var builder = RazorIRBuilder.Create(node);
            builder.Add(new RazorIRToken()
            {
                Content = "i++",
                Kind = RazorIRToken.TokenKind.CSharp,
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

            var node = new CSharpExpressionIRNode();
            var builder = RazorIRBuilder.Create(node);
            builder.Add(new RazorIRToken()
            {
                Content = "i",
                Kind = RazorIRToken.TokenKind.CSharp,
            });
            builder.Add(new MyExtensionIRNode());
            builder.Add(new RazorIRToken()
            {
                Content = "++",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            context.RenderNode = (n) => Assert.IsType<MyExtensionIRNode>(n);

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

            var node = new CSharpExpressionIRNode()
            {
                Source = new SourceSpan("test.cshtml", 8, 0, 8, 3),
            };
            var builder = RazorIRBuilder.Create(node);
            builder.Add(new RazorIRToken()
            {
                Content = "i",
                Kind = RazorIRToken.TokenKind.CSharp,
            });
            builder.Add(new MyExtensionIRNode());
            builder.Add(new RazorIRToken()
            {
                Content = "++",
                Kind = RazorIRToken.TokenKind.CSharp,
            });

            context.RenderNode = (n) => Assert.IsType<MyExtensionIRNode>(n);

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

            var node = new CSharpCodeIRNode();
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
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

            var node = new CSharpCodeIRNode();
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
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

            var node = new CSharpCodeIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 13),
            };
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
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

            var node = new CSharpCodeIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 17),
            };
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
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
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var node = new HtmlContentIRNode();
            node.Children.Add(new RazorIRToken()
            {
                Content = "SomeContent",
                Kind = RazorIRToken.TokenKind.Html,
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
            var writer = new RuntimeBasicWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };


            var node = new HtmlContentIRNode();
            node.Children.Add(new RazorIRToken()
            {
                Content = new string('*', 2000),
                Kind = RazorIRToken.TokenKind.Html,
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
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single();

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
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single().Children[0] as HtmlAttributeValueIRNode;

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
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @false\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single().Children[1] as CSharpExpressionAttributeValueIRNode;

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
            var writer = new RuntimeBasicWriter();
            var context = GetCSharpRenderingContext(writer);

            var content = "<input checked=\"hello-world @if(@true){ }\" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            context.CodeDocument = codeDocument;
            var irDocument = Lower(codeDocument);
            var node = irDocument.Children.OfType<HtmlAttributeIRNode>().Single().Children[1] as CSharpCodeAttributeValueIRNode;

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

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            return Lower(codeDocument, engine);
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetIRDocument();
            Assert.NotNull(irDocument);

            return irDocument;
        }

        private class MyExtensionIRNode : ExtensionIRNode
        {
            public override RazorIRNodeCollection Children => ReadOnlyIRNodeCollection.Instance;

            public override SourceSpan? Source { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Accept(RazorIRNodeVisitor visitor)
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
