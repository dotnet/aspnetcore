// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        public void WriteUsingStatement_WithSource_WritesContentWithLinePragmaAndMapping()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();
            var sourceDocument = TestRazorSourceDocument.Create("@using System;");
            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                CodeDocument = RazorCodeDocument.Create(sourceDocument)
            };

            var originalSpan = new SourceSpan("test.cshtml", 0, 0, 0, 6);
            var generatedSpan = new SourceSpan(null, 23, 1, 0, 6);
            var expectedLineMapping = new LineMapping(originalSpan, generatedSpan);
            var node = new UsingStatementIRNode()
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
                Options = RazorParserOptions.CreateDefaultOptions(),
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
                Options = RazorParserOptions.CreateDefaultOptions(),
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
  __o = i++;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpStatement_WhitespaceContent_DoesNothing()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
            };

            var node = new CSharpStatementIRNode();
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = "  \t"
                });

            // Act
            writer.WriteCSharpStatement(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Empty(csharp);
        }

        [Fact]
        public void WriteCSharpStatement_WhitespaceContentWithSource_WritesContent()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorParserOptions.CreateDefaultOptions(),
            };

            var node = new CSharpStatementIRNode()
            {
                Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            };
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = "    "
                });

            // Act
            writer.WriteCSharpStatement(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"    
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpStatement_SkipsLinePragma_WithoutSource()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
            };

            var node = new CSharpStatementIRNode();
            RazorIRBuilder.Create(node)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = "if (true) { }"
                });

            // Act
            writer.WriteCSharpStatement(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"if (true) { }
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpStatement_WritesLinePragma_WithSource()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorParserOptions.CreateDefaultOptions(),
            };

            var node = new CSharpStatementIRNode()
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
            writer.WriteCSharpStatement(context, node);

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
        public void WriteCSharpStatement_WritesPadding_WithSource()
        {
            // Arrange
            var writer = new DesignTimeBasicWriter();

            var context = new CSharpRenderingContext()
            {
                Writer = new Legacy.CSharpCodeWriter(),
                Options = RazorParserOptions.CreateDefaultOptions(),
            };

            var node = new CSharpStatementIRNode()
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
            writer.WriteCSharpStatement(context, node);

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

        private class MyExtensionIRNode : ExtensionIRNode
        {
            public override IList<RazorIRNode> Children => throw new NotImplementedException();

            public override RazorIRNode Parent { get; set; }
            public override SourceSpan? Source { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override void Accept(RazorIRNodeVisitor visitor)
            {
                throw new NotImplementedException();
            }

            public override void WriteNode(RuntimeTarget target, CSharpRenderingContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
