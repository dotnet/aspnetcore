// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
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

        [Fact]
        public void ModelVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var engine = new RazorTemplateEngine(host);
            var source = ReadResource("TestFiles/Input/Model.cshtml");
            var expectedCode = ReadResource("TestFiles/Output/Model.cs");
            var expectedLineMappings = new List<LineMapping>
            {
                BuildLineMapping(7, 0, 7, 151, 6, 7, 30),
            };

            // Act
            GeneratorResults results;
            using (var buffer = new StringTextBuffer(source))
            {
                results = engine.GenerateCode(buffer);
            }

            // Assert
            Assert.True(results.Success);
            Assert.Equal(expectedCode, results.GeneratedCode);
            Assert.Empty(results.ParserErrors);
            Assert.Equal(expectedLineMappings, results.DesignTimeLineMappings);
        }

        private string ReadResource(string resourceName)
        {
            var assembly = typeof(ModelChunkVisitorTest).Assembly;

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        private static CodeGeneratorContext CreateContext()
        {
            return CodeGeneratorContext.Create(new MvcRazorHost(new TestFileSystem()),
                                              "MyClass",
                                              "MyNamespace",
                                              string.Empty,
                                              shouldGenerateLinePragmas: true);
        }

        private static LineMapping BuildLineMapping(int documentAbsoluteIndex,
                                                    int documentLineIndex,
                                                    int documentCharacterIndex,
                                                    int generatedAbsoluteIndex,
                                                    int generatedLineIndex,
                                                    int generatedCharacterIndex,
                                                    int contentLength)
        {
            var documentLocation = new SourceLocation(documentAbsoluteIndex,
                                                      documentLineIndex,
                                                      documentCharacterIndex);
            var generatedLocation = new SourceLocation(generatedAbsoluteIndex,
                                                       generatedLineIndex,
                                                       generatedCharacterIndex);

            return new LineMapping(
                documentLocation: new MappingLocation(documentLocation, contentLength),
                generatedLocation: new MappingLocation(generatedLocation, contentLength));
        }
    }
}