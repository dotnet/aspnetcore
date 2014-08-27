// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var expected = @"[Microsoft.AspNet.Mvc.ActivateAttribute]
public
#line 1 """"
MyType1 MyPropertyName1

#line default
#line hidden
{ get; private set; }
[Microsoft.AspNet.Mvc.ActivateAttribute]
public
#line 1 """"
MyType2 @MyPropertyName2

#line default
#line hidden
{ get; private set; }
";
            var writer = new CSharpCodeWriter();
            var context = CreateContext();
            context.Host.DesignTimeMode = true;

            var visitor = new InjectChunkVisitor(writer, context, "Microsoft.AspNet.Mvc.ActivateAttribute");
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
        public void InjectVisitor_GeneratesCorrectLineMappings()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var engine = new RazorTemplateEngine(host);
            var source = ReadResource("TestFiles/Input/Inject.cshtml");
            var expectedCode = ReadResource("TestFiles/Output/Inject.cs");
            var expectedLineMappings = new List<LineMapping>
            {
                BuildLineMapping(1, 0, 1, 30, 3, 0, 17),
                BuildLineMapping(28, 1, 8, 598, 26, 8, 20)
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

        [Fact]
        public void InjectVisitorWithModel_GeneratesCorrectLineMappings()
        {
            // Arrange
            var host = new MvcRazorHost(new TestFileSystem())
            {
                DesignTimeMode = true
            };
            host.NamespaceImports.Clear();
            var engine = new RazorTemplateEngine(host);
            var source = ReadResource("TestFiles/Input/InjectWithModel.cshtml");
            var expectedCode = ReadResource("TestFiles/Output/InjectWithModel.cs");
            var expectedLineMappings = new List<LineMapping>
            {
                BuildLineMapping(7, 0, 7, 151, 6, 7, 7),
                BuildLineMapping(24, 1, 8, 587, 26, 8, 20),
                BuildLineMapping(54, 2, 8, 757, 34, 8, 23)
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
            var assembly = typeof(InjectChunkVisitorTest).Assembly;

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