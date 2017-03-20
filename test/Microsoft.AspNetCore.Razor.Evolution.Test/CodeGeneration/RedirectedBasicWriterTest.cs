// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class RedirectedBasicWriterTest
    {
        // In design time this will not include the 'text writer' parameter.
        [Fact]
        public void WriteCSharpExpression_DesignTime_DoesNormalWrite()
        {
            // Arrange
            var writer = new RedirectedBasicWriter(new DesignTimeBasicWriter(), "test_writer")
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
                Writer = new Legacy.CSharpCodeWriter(),
            };

            context.Options.DesignTimeMode = true;

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
        public void WriteCSharpExpression_Runtime_SkipsLinePragma_WithoutSource()
        {
            // Arrange
            var writer = new RedirectedBasicWriter(new DesignTimeBasicWriter(), "test_writer")
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
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
@"Test(test_writer, i++);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_Runtime_WritesLinePragma_WithSource()
        {
            // Arrange
            var writer = new RedirectedBasicWriter(new DesignTimeBasicWriter(), "test_writer")
            {
                WriteCSharpExpressionMethod = "Test",
            };

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
Test(test_writer, i++);

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_Runtime_WithExtensionNode_WritesPadding()
        {
            // Arrange
            var writer = new RedirectedBasicWriter(new DesignTimeBasicWriter(), "test_writer")
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
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
@"Test(test_writer, i++);
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCSharpExpression_Runtime_WithSource_WritesPadding()
        {
            // Arrange
            var writer = new RedirectedBasicWriter(new DesignTimeBasicWriter(), "test_writer")
            {
                WriteCSharpExpressionMethod = "Test",
            };

            var context = new CSharpRenderingContext()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
                SourceDocument = TestRazorSourceDocument.Create("                     @i++"),
                Writer = new Legacy.CSharpCodeWriter(),
            };

            var node = new CSharpExpressionIRNode()
            {
                Source = new SourceSpan("test.cshtml", 24, 0, 24, 3),
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
      Test(test_writer, i++);

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
