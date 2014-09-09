// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Text;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorTemplateEngineTest
    {
        [Fact]
        public void ConstructorRequiresNonNullHost()
        {
            Assert.Throws<ArgumentNullException>("host", () => new RazorTemplateEngine(null));
        }

        [Fact]
        public void ConstructorInitializesHost()
        {
            // Arrange
            RazorEngineHost host = new RazorEngineHost(new CSharpRazorCodeLanguage());

            // Act
            RazorTemplateEngine engine = new RazorTemplateEngine(host);

            // Assert
            Assert.Same(host, engine.Host);
        }

        [Fact]
        public void CreateParserMethodIsConstructedFromHost()
        {
            // Arrange
            RazorEngineHost host = CreateHost();
            RazorTemplateEngine engine = new RazorTemplateEngine(host);

            // Act
            RazorParser parser = engine.CreateParser();

            // Assert
            Assert.IsType<CSharpCodeParser>(parser.CodeParser);
            Assert.IsType<HtmlMarkupParser>(parser.MarkupParser);
        }

        [Fact]
        public void CreateParserMethodSetsParserContextToDesignTimeModeIfHostSetToDesignTimeMode()
        {
            // Arrange
            RazorEngineHost host = CreateHost();
            RazorTemplateEngine engine = new RazorTemplateEngine(host);
            host.DesignTimeMode = true;

            // Act
            RazorParser parser = engine.CreateParser();

            // Assert
            Assert.True(parser.DesignTimeMode);
        }

        [Fact]
        public void CreateParserMethodPassesParsersThroughDecoratorMethodsOnHost()
        {
            // Arrange
            ParserBase expectedCode = new Mock<ParserBase>().Object;
            ParserBase expectedMarkup = new Mock<ParserBase>().Object;

            var mockHost = new Mock<RazorEngineHost>(new CSharpRazorCodeLanguage()) { CallBase = true };
            mockHost.Setup(h => h.DecorateCodeParser(It.IsAny<CSharpCodeParser>()))
                .Returns(expectedCode);
            mockHost.Setup(h => h.DecorateMarkupParser(It.IsAny<HtmlMarkupParser>()))
                .Returns(expectedMarkup);
            RazorTemplateEngine engine = new RazorTemplateEngine(mockHost.Object);

            // Act
            RazorParser actual = engine.CreateParser();

            // Assert
            Assert.Equal(expectedCode, actual.CodeParser);
            Assert.Equal(expectedMarkup, actual.MarkupParser);
        }

        [Fact]
        public void CreateCodeGeneratorMethodPassesCodeGeneratorThroughDecorateMethodOnHost()
        {
            // Arrange
            var mockHost = new Mock<RazorEngineHost>(new CSharpRazorCodeLanguage()) { CallBase = true };

            RazorCodeGenerator expected = new Mock<RazorCodeGenerator>("Foo", "Bar", "Baz", mockHost.Object).Object;

            mockHost.Setup(h => h.DecorateCodeGenerator(It.IsAny<CSharpRazorCodeGenerator>()))
                .Returns(expected);
            RazorTemplateEngine engine = new RazorTemplateEngine(mockHost.Object);

            // Act
            RazorCodeGenerator actual = engine.CreateCodeGenerator("Foo", "Bar", "Baz");

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CreateCodeBuilder_PassesCodeGeneratorThroughDecorateMethodOnHost()
        {
            // Arrange
            var mockHost = new Mock<RazorEngineHost>(new CSharpRazorCodeLanguage()) { CallBase = true };
            var codeBuilderContext = new CodeBuilderContext(
                mockHost.Object,
                "different-class",
                "different-ns",
                string.Empty,
                shouldGenerateLinePragmas: true);

            var expected = new CSharpCodeBuilder(codeBuilderContext);

            mockHost.Setup(h => h.DecorateCodeBuilder(It.IsAny<CSharpCodeBuilder>(), codeBuilderContext))
                    .Returns(expected);
            var engine = new RazorTemplateEngine(mockHost.Object);

            // Act
            var actual = engine.CreateCodeBuilder(codeBuilderContext);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseTemplateCopiesTextReaderContentToSeekableTextReaderAndPassesToParseTemplateCore()
        {
            // Arrange
            Mock<RazorTemplateEngine> mockEngine = new Mock<RazorTemplateEngine>(CreateHost());
            TextReader reader = new StringReader("foo");
            CancellationTokenSource source = new CancellationTokenSource();

            // Act
            mockEngine.Object.ParseTemplate(reader, cancelToken: source.Token);

            // Assert
            mockEngine.Verify(e => e.ParseTemplateCore(It.Is<SeekableTextReader>(l => l.ReadToEnd() == "foo"),
                                                       source.Token));
        }

        [Fact]
        public void GenerateCodeCopiesTextReaderContentToSeekableTextReaderAndPassesToGenerateCodeCore()
        {
            // Arrange
            Mock<RazorTemplateEngine> mockEngine = new Mock<RazorTemplateEngine>(CreateHost());
            TextReader reader = new StringReader("foo");
            CancellationTokenSource source = new CancellationTokenSource();
            string className = "Foo";
            string ns = "Bar";
            string src = "Baz";

            // Act
            mockEngine.Object.GenerateCode(reader, className: className, rootNamespace: ns, sourceFileName: src, cancelToken: source.Token);

            // Assert
            mockEngine.Verify(e => e.GenerateCodeCore(It.Is<SeekableTextReader>(l => l.ReadToEnd() == "foo"),
                                                      className, ns, src, null, source.Token));
        }

        [Fact]
        public void ParseTemplateOutputsResultsOfParsingProvidedTemplateSource()
        {
            // Arrange
            RazorTemplateEngine engine = new RazorTemplateEngine(CreateHost());

            // Act
            ParserResults results = engine.ParseTemplate(new StringTextBuffer("foo @bar("));

            // Assert
            Assert.False(results.Success);
            Assert.Single(results.ParserErrors);
            Assert.NotNull(results.Document);
        }

        [Fact]
        public void GenerateOutputsResultsOfParsingAndGeneration()
        {
            // Arrange
            RazorTemplateEngine engine = new RazorTemplateEngine(CreateHost());

            // Act
            GeneratorResults results = engine.GenerateCode(new StringTextBuffer("foo @bar("));

            // Assert
            Assert.False(results.Success);
            Assert.Single(results.ParserErrors);
            Assert.NotNull(results.Document);
            Assert.NotNull(results.GeneratedCode);
        }

        [Fact]
        public void GenerateOutputsDesignTimeMappingsIfDesignTimeSetOnHost()
        {
            // Arrange
            RazorTemplateEngine engine = new RazorTemplateEngine(CreateHost(designTime: true));

            // Act
            GeneratorResults results = engine.GenerateCode(new StringTextBuffer("foo @bar()"), className: null, rootNamespace: null, sourceFileName: "foo.cshtml");

            // Assert
            Assert.True(results.Success);
            Assert.Empty(results.ParserErrors);
            Assert.NotNull(results.Document);
            Assert.NotNull(results.GeneratedCode);
            Assert.NotNull(results.DesignTimeLineMappings);
        }

        public static IEnumerable<object[]> GenerateCodeCalculatesLinePragma_IfStreamInputIsUsedData
        {
            get
            {
                // Seekable stream
                var content = Encoding.UTF8.GetBytes("Hello world");
                var stream = new MemoryStream(content);

                yield return new[] { stream };

                // Non seekable stream
                var mockStream = new Mock<MemoryStream>(content)
                {
                    CallBase = true
                };
                mockStream.Setup(m => m.CanSeek)
                          .Returns(false);

                yield return new[] { mockStream.Object };
            }
        }

        [Theory]
        [MemberData(nameof(GenerateCodeCalculatesLinePragma_IfStreamInputIsUsedData))]
        public void GenerateCodeCalculatesChecksum_IfStreamInputIsUsed(Stream stream)
        {
            // Arrange
            var engine = new TestableRazorTemplateEngine();

            // Act
            var results = engine.GenerateCode(stream, "some-class", "some-ns", "foo.cshtml");

            // Assert
            Assert.Equal("7b502c3a1f48c8609ae212cdfb639dee39673f5e", engine.Checksum);
        }

        [Fact]
        public void GenerateCode_DoesNotCalculateChecksum_InDesignTimeMode()
        {
            // Arrange
            var engine = new TestableRazorTemplateEngine();
            engine.Host.DesignTimeMode = true;

            // Act
            var results = engine.GenerateCode(Stream.Null, "some-class", "some-ns", "foo.cshtml");

            // Assert
            Assert.Null(engine.Checksum);
        }

        private static RazorEngineHost CreateHost(bool designTime = false)
        {
            return new RazorEngineHost(new CSharpRazorCodeLanguage())
            {
                DesignTimeMode = designTime
            };
        }

        private class TestableRazorTemplateEngine : RazorTemplateEngine
        {
            public TestableRazorTemplateEngine()
                : base(CreateHost())
            {
            }

            public string Checksum { get; set; }

            protected internal override GeneratorResults GenerateCodeCore(ITextDocument input, 
                                                                          string className, 
                                                                          string rootNamespace, 
                                                                          string sourceFileName, 
                                                                          string checksum, 
                                                                          CancellationToken? cancelToken)
            {
                Checksum = checksum;
                return null;
            }
        }
    }
}
