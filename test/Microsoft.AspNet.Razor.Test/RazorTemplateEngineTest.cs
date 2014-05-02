// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;
using System.Threading;
using System.Web.WebPages.TestUtils;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;
using Moq;

namespace Microsoft.AspNet.Razor.Test
{
    public class RazorTemplateEngineTest
    {
        [Fact]
        public void ConstructorRequiresNonNullHost()
        {
            Assert.ThrowsArgumentNull(() => new RazorTemplateEngine(null), "host");
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
                                                      className, ns, src, source.Token));
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

        private static RazorEngineHost CreateHost(bool designTime = false)
        {
            return new RazorEngineHost(new CSharpRazorCodeLanguage())
            {
                DesignTimeMode = designTime
            };
        }
    }
}
