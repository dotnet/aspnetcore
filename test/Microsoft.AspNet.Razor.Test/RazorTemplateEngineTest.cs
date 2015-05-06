// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DNXCORE50
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
using Xunit;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorTemplateEngineTest
    {
        [Fact]
        public void ConstructorInitializesHost()
        {
            // Arrange
            var host = new RazorEngineHost(new CSharpRazorCodeLanguage());

            // Act
            var engine = new RazorTemplateEngine(host);

            // Assert
            Assert.Same(host, engine.Host);
        }

        [Fact]
        public void CreateParserMethodIsConstructedFromHost()
        {
            // Arrange
            var host = CreateHost();
            var engine = new RazorTemplateEngine(host);

            // Act
            var parser = engine.CreateParser("some-file");

            // Assert
            Assert.IsType<CSharpCodeParser>(parser.CodeParser);
            Assert.IsType<HtmlMarkupParser>(parser.MarkupParser);
        }

        [Fact]
        public void CreateParserMethodSetsParserContextToDesignTimeModeIfHostSetToDesignTimeMode()
        {
            // Arrange
            var host = CreateHost();
            var engine = new RazorTemplateEngine(host);
            host.DesignTimeMode = true;

            // Act
            var parser = engine.CreateParser("some-file");

            // Assert
            Assert.True(parser.DesignTimeMode);
        }

        [Fact]
        public void CreateParserMethodPassesParsersThroughDecoratorMethodsOnHost()
        {
            // Arrange
            var expectedCode = new Mock<ParserBase>().Object;
            var expectedMarkup = new Mock<ParserBase>().Object;

            var mockHost = new Mock<RazorEngineHost>(new CSharpRazorCodeLanguage()) { CallBase = true };
            mockHost.Setup(h => h.DecorateCodeParser(It.IsAny<CSharpCodeParser>()))
                .Returns(expectedCode);
            mockHost.Setup(h => h.DecorateMarkupParser(It.IsAny<HtmlMarkupParser>()))
                .Returns(expectedMarkup);
            var engine = new RazorTemplateEngine(mockHost.Object);

            // Act
            var actual = engine.CreateParser("some-file");

            // Assert
            Assert.Equal(expectedCode, actual.CodeParser);
            Assert.Equal(expectedMarkup, actual.MarkupParser);
        }

        [Fact]
        public void CreateCodeGeneratorMethodPassesCodeGeneratorThroughDecorateMethodOnHost()
        {
            // Arrange
            var mockHost = new Mock<RazorEngineHost>(new CSharpRazorCodeLanguage()) { CallBase = true };

            var expected = new Mock<RazorCodeGenerator>("Foo", "Bar", "Baz", mockHost.Object).Object;

            mockHost.Setup(h => h.DecorateCodeGenerator(It.IsAny<CSharpRazorCodeGenerator>()))
                .Returns(expected);
            var engine = new RazorTemplateEngine(mockHost.Object);

            // Act
            var actual = engine.CreateCodeGenerator("Foo", "Bar", "Baz");

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
                shouldGenerateLinePragmas: true,
                errorSink: new ErrorSink());

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
            var mockEngine = new Mock<RazorTemplateEngine>(CreateHost());
            var reader = new StringReader("foo");
            var source = new CancellationTokenSource();

            // Act
            mockEngine.Object.ParseTemplate(reader, cancelToken: source.Token);

            // Assert
            mockEngine.Verify(e => e.ParseTemplateCore(It.Is<SeekableTextReader>(l => l.ReadToEnd() == "foo"),
                                                       null,
                                                       source.Token));
        }

        [Fact]
        public void GenerateCodeCopiesTextReaderContentToSeekableTextReaderAndPassesToGenerateCodeCore()
        {
            // Arrange
            var mockEngine = new Mock<RazorTemplateEngine>(CreateHost());
            var reader = new StringReader("foo");
            var source = new CancellationTokenSource();
            var className = "Foo";
            var ns = "Bar";
            var src = "Baz";

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
            var engine = new RazorTemplateEngine(CreateHost());

            // Act
            var results = engine.ParseTemplate(new StringTextBuffer("foo @bar("));

            // Assert
            Assert.False(results.Success);
            Assert.Single(results.ParserErrors);
            Assert.NotNull(results.Document);
        }

        [Fact]
        public void GenerateOutputsResultsOfParsingAndGeneration()
        {
            // Arrange
            var engine = new RazorTemplateEngine(CreateHost());

            // Act
            var results = engine.GenerateCode(new StringTextBuffer("foo @bar("));

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
            var engine = new RazorTemplateEngine(CreateHost(designTime: true));

            // Act
            var results = engine.GenerateCode(new StringTextBuffer("foo @bar()"), className: null, rootNamespace: null, sourceFileName: "foo.cshtml");

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

        [Fact]
        public void GenerateCode_UsesDecoratedRazorParser()
        {
            // Arrange
            Mock<RazorParser> parser = null;
            var host = new Mock<RazorEngineHost>(new CSharpRazorCodeLanguage())
            {
                CallBase = true
            };
            host.Setup(p => p.DecorateRazorParser(It.IsAny<RazorParser>(), "foo.cshtml"))
                .Returns((RazorParser p, string file) =>
                {
                    parser = new Mock<RazorParser>(p)
                    {
                        CallBase = true
                    };
                    return parser.Object;
                })
                .Verifiable();

            var engine = new RazorTemplateEngine(host.Object);

            // Act
            var results = engine.GenerateCode(Stream.Null, "some-class", "some-ns", "foo.cshtml");

            // Assert
            Assert.NotNull(parser);

            parser.Verify(v => v.Parse(It.IsAny<ITextDocument>()), Times.Once());
            host.Verify();
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
#endif
