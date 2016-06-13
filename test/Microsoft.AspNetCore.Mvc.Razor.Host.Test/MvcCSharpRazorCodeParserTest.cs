// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.Generator;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Text;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
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
                    .As(new ModelChunkGenerator("Foo"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
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
        public void ParseModelKeyword_InfersBaseType_FromModelName(
            string modelName,
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
                factory.Code(modelName + Environment.NewLine)
                    .As(new ModelChunkGenerator(expectedModel))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.Markup("Bar")
                    .With(new MarkupChunkGenerator())
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
                    .As(new ModelChunkGenerator(string.Empty))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml(),
            };
            var expectedErrors = new[]
            {
                new RazorError("The 'model' keyword must be followed by a type name on the same line.",
                               new SourceLocation(1, 0, 1), 5)
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
                factory.Code("Foo" + Environment.NewLine)
                    .As(new ModelChunkGenerator("Foo"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Bar")
                    .As(new ModelChunkGenerator("Bar"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
            };

            var expectedErrors = new[]
            {
                new RazorError(
                    "Only one 'model' statement is allowed in a file.",
                    PlatformNormalizer.NormalizedSourceLocation(13, 1, 1),
                    5)
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
                factory.Code("Foo" + Environment.NewLine)
                    .As(new ModelChunkGenerator("Foo"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inherits ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Bar")
                    .As(new SetBaseTypeChunkGenerator("Bar"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
            };

            var expectedErrors = new[]
            {
                new RazorError(
                    "The 'inherits' keyword is not allowed when a 'model' keyword is used.",
                    PlatformNormalizer.NormalizedSourceLocation(21, 1, 9),
                    length: 8)
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
                    .As(new SetBaseTypeChunkGenerator("Bar"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("model ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("Foo")
                    .As(new ModelChunkGenerator("Foo"))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
            };

            var expectedErrors = new[]
            {
                new RazorError(
                    "The 'inherits' keyword is not allowed when a 'model' keyword is used.",
                    new SourceLocation(9, 0, 9),
                    length: 8)
            };

            // Act
            var spans = ParseDocument(document, errors);

            // Assert
            Assert.Equal(expectedSpans, spans.ToArray());
            Assert.Equal(expectedErrors, errors.ToArray());
        }

        [Theory]
        [InlineData("IMyService Service", "IMyService", "Service")]
        [InlineData("  Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>  MyHelper  ",
                    "Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>", "MyHelper")]
        [InlineData("    TestService    @class ", "TestService", "@class")]
        public void ParseInjectKeyword_InfersTypeAndPropertyName(
            string injectStatement,
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
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("IMyService Service;", "IMyService", "Service")]
        [InlineData("IMyService Service;;", "IMyService", "Service")]
        [InlineData("  Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>  MyHelper;  ",
                    "Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>", "MyHelper")]
        [InlineData("  Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>  MyHelper;  ;  ",
                    "Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>", "MyHelper")]
        [InlineData("    TestService    @class; ; ", "TestService", "@class")]
        [InlineData("IMyService Service  ;", "IMyService", "Service")]
        [InlineData("IMyService Service  ;  ;", "IMyService", "Service")]
        [InlineData("  Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>  MyHelper  ;  ",
                    "Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>", "MyHelper")]
        [InlineData("  Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>  MyHelper  ;  ;  ",
                    "Microsoft.AspNetCore.Mvc.IHtmlHelper<MyNullableModel[]?>", "MyHelper")]
        [InlineData("    TestService    @class  ; ", "TestService", "@class")]
        [InlineData("    TestService    @class  ; ; ", "TestService", "@class")]
        public void ParseInjectKeyword_AllowsOptionalTrailingSemicolon(
            string injectStatement,
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
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
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
        public void ParseInjectKeyword_ParsesUpToNewLine(
            string injectStatement,
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
                factory.Code(injectStatement + Environment.NewLine)
                    .As(new InjectParameterGenerator(expectedService, expectedPropertyName))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.Markup("Bar")
                    .With(new MarkupChunkGenerator())
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
            var documentContent = $"@inject    {Environment.NewLine}Bar";
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("   " + Environment.NewLine)
                    .As(new InjectParameterGenerator(string.Empty, string.Empty))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.Markup("Bar")
                    .With(new MarkupChunkGenerator())
            };
            var expectedErrors = new[]
            {
                new RazorError("The 'inject' keyword must be followed by a type name on the same line.",
                                new SourceLocation(1, 0, 1), 6)
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
                    .As(new InjectParameterGenerator(string.Empty, string.Empty))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
            };
            var expectedErrors = new[]
            {
                 new RazorError("The 'inject' keyword must be followed by a type name on the same line.",
                                new SourceLocation(1, 0, 1), 6)
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
            var documentContent = $"@inject   IMyService  {Environment.NewLine}Bar";
            var factory = SpanFactory.CreateCsHtml();
            var expectedSpans = new Span[]
            {
                factory.EmptyHtml(),
                factory.CodeTransition(SyntaxConstants.TransitionString)
                    .Accepts(AcceptedCharacters.None),
                factory.MetaCode("inject ")
                    .Accepts(AcceptedCharacters.None),
                factory.Code("  IMyService  " + Environment.NewLine)
                    .As(new InjectParameterGenerator("IMyService", string.Empty))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.Markup("Bar")
                    .With(new MarkupChunkGenerator())
            };
            var expectedErrors = new[]
            {
                new RazorError("A property name must be specified when using the 'inject' statement. " +
                               "Format for a 'inject' statement is '@inject <Type Name> <Property Name>'.",
                                new SourceLocation(1, 0, 1), 21)
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
                    .As(new InjectParameterGenerator("IMyServi", string.Empty))
                    .Accepts(AcceptedCharacters.AnyExceptNewline),
                factory.EmptyHtml()
            };
            var expectedErrors = new[]
            {
                new RazorError("A property name must be specified when using the 'inject' statement. " +
                               "Format for a 'inject' statement is '@inject <Type Name> <Property Name>'.",
                                new SourceLocation(1, 0, 1), 21)
            };

            // Act
            var spans = ParseDocument(documentContent, errors);

            // Assert
            Assert.Equal(expectedSpans, spans);
            Assert.Equal(expectedErrors, errors);
        }

        private static List<Span> ParseDocument(
            string documentContents,
            List<RazorError> errors = null,
            List<LineMapping> lineMappings = null)
        {
            errors = errors ?? new List<RazorError>();
            var markupParser = new HtmlMarkupParser();
            var codeParser = new TestMvcCSharpRazorCodeParser();
            var reader = new SeekableTextReader(documentContents);
            var context = new ParserContext(
                reader,
                codeParser,
                markupParser,
                markupParser,
                new ErrorSink());
            codeParser.Context = context;
            markupParser.Context = context;
            markupParser.ParseDocument();

            var results = context.CompleteParse();
            errors.AddRange(results.ParserErrors);
            return results.Document.Flatten().ToList();
        }

        private sealed class TestMvcCSharpRazorCodeParser : MvcRazorCodeParser
        {
            public bool HasDirective(string directive)
            {
                Action handler;
                return TryGetDirectiveHandler(directive, out handler);
            }
        }
    }
}
