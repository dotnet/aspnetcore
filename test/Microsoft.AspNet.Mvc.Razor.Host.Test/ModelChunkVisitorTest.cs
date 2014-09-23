// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ModelChunkVisitorTest
    {
        [Fact]
        public void Visit_IgnoresNonModelChunks()
        {
            // Arrange
            var writer = new CSharpCodeWriter();
            var context = CreateContext();

            var visitor = new ModelChunkVisitor(writer, context);

            // Act
            visitor.Accept(new Chunk[]
            {
                new LiteralChunk(),
                new CodeAttributeChunk()
            });
            var code = writer.GenerateCode();

            // Assert
            Assert.Empty(code);
        }

        [Fact]
        public void Visit_GeneratesBaseClass_ForModelChunks()
        {
            // Arrange
            var expected =
"MyBase<" + Environment.NewLine +
"#line 1 \"\"" + Environment.NewLine +
"My_Generic.After.Periods" + Environment.NewLine +
Environment.NewLine +
"#line default" + Environment.NewLine +
"#line hidden" + Environment.NewLine +
">";
            var writer = new CSharpCodeWriter();
            var context = CreateContext();

            var visitor = new ModelChunkVisitor(writer, context);
            var factory = SpanFactory.CreateCsHtml();
            var node = (Span)factory.Code("Some code")
                                    .As(new ModelCodeGenerator("MyBase", "MyGeneric"));

            // Act
            visitor.Accept(new Chunk[]
            {
                new LiteralChunk(),
                new ModelChunk("MyBase", "My_Generic.After.Periods") { Association = node }
            });
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        [Fact]
        public void Visit_WithDesignTimeHost_GeneratesBaseClass_ForModelChunks()
        {
            // Arrange
            var expected =
"MyBase<" + Environment.NewLine +
"#line 1 \"\"" + Environment.NewLine +
"My_Generic.After.Periods" + Environment.NewLine +
Environment.NewLine +
"#line default" + Environment.NewLine +
"#line hidden" + Environment.NewLine +
">";
            var writer = new CSharpCodeWriter();
            var context = CreateContext();
            context.Host.DesignTimeMode = true;

            var visitor = new ModelChunkVisitor(writer, context);
            var factory = SpanFactory.CreateCsHtml();
            var node = (Span)factory.Code("Some code")
                                    .As(new ModelCodeGenerator("MyType", "MyPropertyName"));

            // Act
            visitor.Accept(new Chunk[]
            {
                new LiteralChunk(),
                new ModelChunk("MyBase", "My_Generic.After.Periods") { Association = node }
            });
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        private static CodeBuilderContext CreateContext()
        {
            return new CodeBuilderContext(
                new CodeGeneratorContext(new MvcRazorHost(new TestFileSystem()),
                                         "MyClass",
                                         "MyNamespace",
                                         string.Empty,
                                         shouldGenerateLinePragmas: true));
        }
    }
}