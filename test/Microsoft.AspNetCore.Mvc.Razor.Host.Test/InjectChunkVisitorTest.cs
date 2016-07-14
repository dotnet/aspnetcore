// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Directives;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class InjectChunkVisitorTest
    {
        [Fact]
        public void Visit_IgnoresNonInjectChunks()
        {
            // Arrange
            var writer = new CSharpCodeWriter();
            var context = CreateContext();

            var visitor = new InjectChunkVisitor(writer, context, "ActivateAttribute");

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
        public void Visit_GeneratesProperties_ForInjectChunks()
        {
            // Arrange
            var expected =
@"[ActivateAttribute]
public MyType1 MyPropertyName1 { get; private set; }
[ActivateAttribute]
public MyType2 @MyPropertyName2 { get; private set; }
";
            var writer = new CSharpCodeWriter();
            var context = CreateContext();

            var visitor = new InjectChunkVisitor(writer, context, "ActivateAttribute");
            var factory = SpanFactory.CreateCsHtml();
            var node = (Span)factory.Code("Some code")
                                    .As(new InjectParameterGenerator("MyType", "MyPropertyName"));

            // Act
            visitor.Accept(new Chunk[]
            {
                new LiteralChunk(),
                new InjectChunk("MyType1", "MyPropertyName1") { Association = node },
                new InjectChunk("MyType2", "@MyPropertyName2") { Association = node }
            });
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        [Fact]
        public void Visit_WithDesignTimeHost_GeneratesPropertiesAndLinePragmas_ForInjectChunks()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
"[Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]",
"public",
@"#line 1 """"",
"MyType1 MyPropertyName1",
"",
"#line default",
"#line hidden",
"{ get; private set; }",
"[Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]",
"public",
@"#line 1 """"",
"MyType2 @MyPropertyName2",
"",
"#line default",
"#line hidden",
"{ get; private set; }",
"");
            var writer = new CSharpCodeWriter();
            var context = CreateContext();
            context.Host.DesignTimeMode = true;

            var visitor = new InjectChunkVisitor(writer, context, "Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute");
            var factory = SpanFactory.CreateCsHtml();
            var node = (Span)factory.Code("Some code")
                                    .As(new InjectParameterGenerator("MyType", "MyPropertyName"));

            // Act
            visitor.Accept(new Chunk[]
            {
                new LiteralChunk(),
                new InjectChunk("MyType1", "MyPropertyName1") { Association = node },
                new InjectChunk("MyType2", "@MyPropertyName2") { Association = node }
            });
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        [Fact]
        public void Visit_WithDesignTimeHost_GeneratesPropertiesAndLinePragmas_ForPartialInjectChunks()
        {
            // Arrange
            var expected = @"[Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
public
#line 1 """"
MyType1

#line default
#line hidden
{ get; private set; }
";
            var writer = new CSharpCodeWriter();
            var context = CreateContext();
            context.Host.DesignTimeMode = true;

            var visitor = new InjectChunkVisitor(writer, context, "Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute");
            var factory = SpanFactory.CreateCsHtml();
            var node = (Span)factory.Code("Some code")
                                    .As(new InjectParameterGenerator("MyType", "MyPropertyName"));

            // Act
            visitor.Accept(new Chunk[]
            {
                new LiteralChunk(),
                new InjectChunk("MyType1", string.Empty) { Association = node },
            });
            var code = writer.GenerateCode();

            // Assert
            Assert.Equal(expected, code);
        }

        private static CodeGeneratorContext CreateContext()
        {
            var chunkTreeCache = new DefaultChunkTreeCache(new TestFileProvider());
            return new CodeGeneratorContext(
                new ChunkGeneratorContext(
                    new MvcRazorHost(chunkTreeCache, new TagHelperDescriptorResolver(designTime: false)),
                    "MyClass",
                    "MyNamespace",
                    string.Empty,
                    shouldGenerateLinePragmas: true),
                new ErrorSink());
        }
    }
}