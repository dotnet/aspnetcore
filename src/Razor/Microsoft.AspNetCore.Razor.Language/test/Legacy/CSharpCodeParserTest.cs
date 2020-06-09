// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test.Legacy
{
    public class CSharpCodeParserTest
    {
        public static TheoryData InvalidTagHelperPrefixData
        {
            get
            {
                var directiveLocation = new SourceLocation(1, 2, 3);

                RazorDiagnostic InvalidPrefixError(int length, char character, string prefix)
                {
                    return RazorDiagnosticFactory.CreateParsing_InvalidTagHelperPrefixValue(
                        new SourceSpan(directiveLocation, length), SyntaxConstants.CSharp.TagHelperPrefixKeyword, character, prefix);
                }

                return new TheoryData<string, SourceLocation, IEnumerable<RazorDiagnostic>>
                {
                    {
                        "th ",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, ' ', "th "),
                        }
                    },
                    {
                        "th\t",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, '\t', "th\t"),
                        }
                    },
                    {
                        "th" + Environment.NewLine,
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(2 + Environment.NewLine.Length, Environment.NewLine[0], "th" + Environment.NewLine),
                        }
                    },
                    {
                        " th ",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(4, ' ', " th "),
                        }
                    },
                    {
                        "@",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(1, '@', "@"),
                        }
                    },
                    {
                        "t@h",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, '@', "t@h"),
                        }
                    },
                    {
                        "!",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(1, '!', "!"),
                        }
                    },
                    {
                        "!th",
                        directiveLocation,
                        new[]
                        {
                            InvalidPrefixError(3, '!', "!th"),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidTagHelperPrefixData))]
        public void ValidateTagHelperPrefix_ValidatesPrefix(
            string directiveText,
            SourceLocation directiveLocation,
            object expectedErrors)
        {
            // Arrange
            var expectedDiagnostics = (IEnumerable<RazorDiagnostic>)expectedErrors;
            var source = TestRazorSourceDocument.Create();
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var parser = new CSharpCodeParser(context);
            var diagnostics = new List<RazorDiagnostic>();

            // Act
            parser.ValidateTagHelperPrefix(directiveText, directiveLocation, diagnostics);

            // Assert
            Assert.Equal(expectedDiagnostics, diagnostics);
        }

        [Theory]
        [InlineData("foo,assemblyName", 4)]
        [InlineData("foo, assemblyName", 5)]
        [InlineData("   foo, assemblyName", 8)]
        [InlineData("   foo   , assemblyName", 11)]
        [InlineData("foo,    assemblyName", 8)]
        [InlineData("   foo   ,    assemblyName   ", 14)]
        public void ParseAddOrRemoveDirective_CalculatesAssemblyLocationInLookupText(string text, int assemblyLocation)
        {
            // Arrange
            var source = TestRazorSourceDocument.Create();
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var parser = new CSharpCodeParser(context);

            var directive = new CSharpCodeParser.ParsedDirective()
            {
                DirectiveText = text,
            };

            var diagnostics = new List<RazorDiagnostic>();
            var expected = new SourceLocation(assemblyLocation, 0, assemblyLocation);

            // Act
            var result = parser.ParseAddOrRemoveDirective(directive, SourceLocation.Zero, diagnostics);

            // Assert
            Assert.Empty(diagnostics);
            Assert.Equal("foo", result.TypePattern);
            Assert.Equal("assemblyName", result.AssemblyName);
        }

        [Theory]
        [InlineData("", 1)]
        [InlineData("*,", 2)]
        [InlineData("?,", 2)]
        [InlineData(",", 1)]
        [InlineData(",,,", 3)]
        [InlineData("First, ", 7)]
        [InlineData("First , ", 8)]
        [InlineData(" ,Second", 8)]
        [InlineData(" , Second", 9)]
        [InlineData("SomeType,", 9)]
        [InlineData("SomeAssembly", 12)]
        [InlineData("First,Second,Third", 18)]
        public void ParseAddOrRemoveDirective_CreatesErrorIfInvalidLookupText_DoesNotThrow(string directiveText, int errorLength)
        {
            // Arrange
            var source = TestRazorSourceDocument.Create();
            var options = RazorParserOptions.CreateDefault();
            var context = new ParserContext(source, options);

            var parser = new CSharpCodeParser(context);

            var directive = new CSharpCodeParser.ParsedDirective()
            {
                DirectiveText = directiveText
            };

            var diagnostics = new List<RazorDiagnostic>();
            var expectedError = RazorDiagnosticFactory.CreateParsing_InvalidTagHelperLookupText(
                new SourceSpan(new SourceLocation(1, 2, 3), errorLength), directiveText);

            // Act
            var result = parser.ParseAddOrRemoveDirective(directive, new SourceLocation(1, 2, 3), diagnostics);

            // Assert
            Assert.Same(directive, result);

            var error = Assert.Single(diagnostics);
            Assert.Equal(expectedError, error);
        }

        [Fact]
        public void TagHelperPrefixDirective_DuplicatesCauseError()
        {
            // Arrange
            var expectedDiagnostic = RazorDiagnosticFactory.CreateParsing_DuplicateDirective(
                new SourceSpan(null, 22 + Environment.NewLine.Length, 1, 0, 16), "tagHelperPrefix");
            var source = TestRazorSourceDocument.Create(
                @"@tagHelperPrefix ""th:""
@tagHelperPrefix ""th""",
                filePath: null);

            // Act
            var document = RazorSyntaxTree.Parse(source);

            // Assert
            var erroredNode = document.Root.DescendantNodes().Last(n => n.GetSpanContext()?.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator);
            var chunkGenerator = Assert.IsType<TagHelperPrefixDirectiveChunkGenerator>(erroredNode.GetSpanContext().ChunkGenerator);
            var diagnostic = Assert.Single(chunkGenerator.Diagnostics);
            Assert.Equal(expectedDiagnostic, diagnostic);
        }
    }
}
