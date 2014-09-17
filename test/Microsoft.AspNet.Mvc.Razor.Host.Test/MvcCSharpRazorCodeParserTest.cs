// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcCSharpRazorCodeParserTest
    {
        [Theory]
        [InlineData("model")]
        [InlineData("inject")]
        public void Constructor_AddsMvcSpecificKeywords(string keyword)
        {
            // Arrange
            var parser = new TestMvcCSharpRazorCodeParser();

            // Act
            var hasDirective = parser.HasDirective(keyword);

            // Assert
            Assert.True(hasDirective);
        }

        [Fact]
        public void ParseModelKeyword_HandlesSingleInstance()
        {
            // Arrange
            var document = "@model    Foo";
            var factory = SpanFactory.CreateCsHtml();
            var errors = new List<RazorError>();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("   Foo")
                    .As(new ModelCodeGenerator("RazorView", "Foo"))
            };

            // Act
            var spans = ParseDocument(document, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("Foo?", "Foo?")]
        [InlineData("Foo[[]][]", "Foo[[]][]")]
        [InlineData("$rootnamespace$.MyModel", "$rootnamespace$.MyModel")]
        public void ParseModelKeyword_InfersBaseType_FromModelName(string modelName,
                                                                   string expectedModel)
        {
            // Arrange
            var documentContent = "@model " + modelName + Environment.NewLine + "Bar";
            var factory = SpanFactory.CreateCsHtml();
            var errors = new List<RazorError>();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code(modelName + "\r\n")
                    .As(new ModelCodeGenerator("RazorView", expectedModel)),
                factory.Markup("Bar")
                    .With(new MarkupCodeGenerator())
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Empty(errors);
        }

        [Fact]
        public void ParseModelKeyword_ErrorOnMissingModelType()
        {
            // Arrange + Act
            var errors = new List<RazorError>();
            var document = "@model   ";
            var spans = ParseDocument(document, errors);

            // Assert
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("  ")
                    .As(new ModelCodeGenerator("RazorView", string.Empty)),
            };
            var expectedErrors = new[]
            {
                new RazorError("The 'model' keyword must be followed by a type name on the same line.",
                               new SourceLocation(9, 0, 9), 1)
            };
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        [Fact]
        public void ParseModelKeyword_ErrorOnMultipleModelStatements()
        {
            // Arrange + Act
            var errors = new List<RazorError>();
            var document =
                "@model Foo" + Environment.NewLine
              + "@model Bar";

            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Foo\r\n")
                    .As(new ModelCodeGenerator("RazorView", "Foo")),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Bar")
                    .As(new ModelCodeGenerator("RazorView", "Bar"))
            };

            var expectedErrors = new[]
            {
                new RazorError("Only one 'model' statement is allowed in a file.",
                                new SourceLocation(18, 1, 6), 1)
            };

            // Act
            var spans = ParseDocument(document, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        [Fact]
        public void ParseModelKeyword_ErrorOnModelFollowedByInherits()
        {
            // Arrange
            var errors = new List<RazorError>();
            var document =
                "@model Foo" + Environment.NewLine
              + "@inherits Bar";

            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Foo\r\n")
                    .As(new ModelCodeGenerator("RazorView", "Foo")),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inherits ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Bar")
                    .As(new SetBaseTypeCodeGenerator("Bar"))
            };

            var expectedErrors = new[]
            {
                new RazorError("The 'inherits' keyword is not allowed when a 'model' keyword is used.",
                               new SourceLocation(21, 1, 9), 1)
            };

            // Act
            var spans = ParseDocument(document, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        [Fact]
        public void ParseModelKeyword_ErrorOnInheritsFollowedByModel()
        {
            // Arrange
            var errors = new List<RazorError>();
            var document =
                "@inherits Bar" + Environment.NewLine
              + "@model Foo";

            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inherits ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Bar" + Environment.NewLine)
                    .As(new SetBaseTypeCodeGenerator("Bar")),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Foo")
                    .As(new ModelCodeGenerator("RazorView", "Foo"))
            };

            var expectedErrors = new[]
            {
                new RazorError("The 'inherits' keyword is not allowed when a 'model' keyword is used.",
                               new SourceLocation(9, 0, 9), 1)
            };

            // Act
            var spans = ParseDocument(document, errors);

            // Assert
            Assert.Equal(expectedSpans, spans.ToArray());
            Assert.Equal(expectedErrors, errors.ToArray());
        }

        [Theory]
        [InlineData("IMyService Service", "IMyService", "Service")]
        [InlineData("  Microsoft.AspNet.Mvc.IHtmlHelper<MyNullableModel[]?>  MyHelper  ",
                    "Microsoft.AspNet.Mvc.IHtmlHelper<MyNullableModel[]?>", "MyHelper")]
        [InlineData("    TestService    @class ", "TestService", "@class")]
        public void ParseInjectKeyword_InfersTypeAndPropertyName(string injectStatement,
                                                                 string expectedService,
                                                                 string expectedPropertyName)
        {
            // Arrange
            var documentContent = "@inject " + injectStatement;
            var factory = SpanFactory.CreateCsHtml();
            var errors = new List<RazorError>();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code(injectStatement)
                    .As(new InjectParameterGenerator(expectedService, expectedPropertyName))
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("IMyService              Service                ", "IMyService", "Service")]
        [InlineData("           TestService    @namespace  ", "TestService", "@namespace")]
        public void ParseInjectKeyword_ParsesUpToNewLine(string injectStatement,
                                                         string expectedService,
                                                         string expectedPropertyName)
        {
            // Arrange
            var documentContent = "@inject " + injectStatement + Environment.NewLine + "Bar";
            var factory = SpanFactory.CreateCsHtml();
            var errors = new List<RazorError>();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code(injectStatement + "\r\n")
                    .As(new InjectParameterGenerator(expectedService, expectedPropertyName)),
                factory.Markup("Bar")
                    .With(new MarkupCodeGenerator())
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Empty(errors);
        }

        [Fact]
        public void ParseInjectKeyword_ErrorOnMissingTypeName()
        {
            // Arrange
            var errors = new List<RazorError>();
            var documentContent = "@inject    " + Environment.NewLine + "Bar";
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("   \r\n")
                    .As(new InjectParameterGenerator(string.Empty, string.Empty)),
                factory.Markup("Bar")
                    .With(new MarkupCodeGenerator())
            };
            var expectedErrors = new[]
            {
                new RazorError("The 'inject' keyword must be followed by a type name on the same line.",
                                new SourceLocation(11, 0, 11), 1)
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        [Fact]
        public void ParseInjectKeyword_ErrorOnMissingTypeName_WhenTypeNameEndsWithEOF()
        {
            // Arrange
            var errors = new List<RazorError>();
            var documentContent = "@inject    ";
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("   ")
                    .As(new InjectParameterGenerator(string.Empty, string.Empty)),
            };
            var expectedErrors = new[]
            {
                 new RazorError("The 'inject' keyword must be followed by a type name on the same line.",
                                new SourceLocation(11, 0, 11), 1)
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        [Fact]
        public void ParseInjectKeyword_ErrorOnMissingPropertyName()
        {
            // Arrange
            var errors = new List<RazorError>();
            var documentContent = "@inject   IMyService  \r\nBar";
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("  IMyService  \r\n")
                    .As(new InjectParameterGenerator("IMyService", string.Empty)),
                factory.Markup("Bar")
                    .With(new MarkupCodeGenerator())
            };
            var expectedErrors = new[]
            {
                new RazorError("A property name must be specified when using the 'inject' statement. " +
                               "Format for a 'inject' statement is '@inject <Type Name> <Property Name>'.",
                                new SourceLocation(20, 0, 20), 1)
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        [Fact]
        public void ParseInjectKeyword_ErrorOnMissingPropertyName_WhenTypeNameEndsWithEOF()
        {
            // Arrange
            var errors = new List<RazorError>();
            var documentContent = "@inject    IMyServi";
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("   IMyServi")
                    .As(new InjectParameterGenerator("IMyServi", string.Empty)),
            };
            var expectedErrors = new[]
            {
                new RazorError("A property name must be specified when using the 'inject' statement. " +
                               "Format for a 'inject' statement is '@inject <Type Name> <Property Name>'.",
                                new SourceLocation(19, 0, 19), 1)
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        private static List<Span> ParseDocument(string documentContents,
                                                List<RazorError> errors = null,
                                                List<LineMapping> lineMappings = null)
        {
            errors = errors ?? new List<RazorError>();
            var markupParser = new HtmlMarkupParser();
            var codeParser = new TestMvcCSharpRazorCodeParser();
            var reader = new SeekableTextReader(documentContents);
            var context = new ParserContext(reader, codeParser, markupParser, markupParser);
            codeParser.Context = context;
            markupParser.Context = context;
            markupParser.ParseDocument();

            var results = context.CompleteParse();
            errors.AddRange(results.ParserErrors);
            return results.Document.Flatten().ToList();
        }

        private sealed class TestMvcCSharpRazorCodeParser : MvcRazorCodeParser
        {
            public TestMvcCSharpRazorCodeParser()
                : base("RazorView")
            {
            }

            public bool HasDirective(string directive)
            {
                Action handler;
                return TryGetDirectiveHandler(directive, out handler);
            }
        }
    }
}
