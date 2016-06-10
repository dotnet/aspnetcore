// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.Editor;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols;
using Microsoft.AspNetCore.Razor.Tokenizer.Symbols.Internal;

namespace Microsoft.AspNetCore.Razor.Parser
{
    public partial class CSharpCodeParser
    {
        private void SetupDirectives()
        {
            MapDirectives(TagHelperPrefixDirective, SyntaxConstants.CSharp.TagHelperPrefixKeyword);
            MapDirectives(AddTagHelperDirective, SyntaxConstants.CSharp.AddTagHelperKeyword);
            MapDirectives(RemoveTagHelperDirective, SyntaxConstants.CSharp.RemoveTagHelperKeyword);
            MapDirectives(InheritsDirective, SyntaxConstants.CSharp.InheritsKeyword);
            MapDirectives(FunctionsDirective, SyntaxConstants.CSharp.FunctionsKeyword);
            MapDirectives(SectionDirective, SyntaxConstants.CSharp.SectionKeyword);
        }

        protected virtual void TagHelperPrefixDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                prefix => new TagHelperPrefixDirectiveChunkGenerator(prefix));
        }

        protected virtual void AddTagHelperDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.AddTagHelperKeyword,
                lookupText => new AddTagHelperChunkGenerator(lookupText));
        }

        protected virtual void RemoveTagHelperDirective()
        {
            TagHelperDirective(
                SyntaxConstants.CSharp.RemoveTagHelperKeyword,
                lookupText => new RemoveTagHelperChunkGenerator(lookupText));
        }

        protected virtual void SectionDirective()
        {
            var nested = Context.IsWithin(BlockType.Section);
            var errorReported = false;

            // Set the block and span type
            Context.CurrentBlock.Type = BlockType.Section;

            // Verify we're on "section" and accept
            AssertDirective(SyntaxConstants.CSharp.SectionKeyword);
            var startLocation = CurrentLocation;
            AcceptAndMoveNext();

            if (nested)
            {
                Context.OnError(
                    startLocation,
                    RazorResources.FormatParseError_Sections_Cannot_Be_Nested(RazorResources.SectionExample_CS),
                    Span.GetContent().Value.Length);
                errorReported = true;
            }

            var whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            // Get the section name
            var sectionName = string.Empty;
            if (!Required(CSharpSymbolType.Identifier,
                          errorIfNotFound: true,
                          errorBase: RazorResources.FormatParseError_Unexpected_Character_At_Section_Name_Start))
            {
                if (!errorReported)
                {
                    errorReported = true;
                }

                PutCurrentBack();
                PutBack(whitespace);
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: false));
            }
            else
            {
                Accept(whitespace);
                sectionName = CurrentSymbol.Content;
                AcceptAndMoveNext();
            }
            Context.CurrentBlock.ChunkGenerator = new SectionChunkGenerator(sectionName);

            var errorLocation = CurrentLocation;
            whitespace = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            // Get the starting brace
            var sawStartingBrace = At(CSharpSymbolType.LeftBrace);
            if (!sawStartingBrace)
            {
                if (!errorReported)
                {
                    errorReported = true;
                    Context.OnError(
                        errorLocation,
                        RazorResources.ParseError_MissingOpenBraceAfterSection,
                        length: 1  /* { */);
                }

                PutCurrentBack();
                PutBack(whitespace);
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: false));
                Optional(CSharpSymbolType.NewLine);
                Output(SpanKind.MetaCode);
                CompleteBlock();
                return;
            }
            else
            {
                Accept(whitespace);
            }

            var startingBraceLocation = CurrentLocation;

            // Set up edit handler
            var editHandler = new AutoCompleteEditHandler(Language.TokenizeString, autoCompleteAtEndOfSpan: true);

            Span.EditHandler = editHandler;
            Span.Accept(CurrentSymbol);

            // Output Metacode then switch to section parser
            Output(SpanKind.MetaCode);
            SectionBlock("{", "}", caseSensitive: true);

            Span.ChunkGenerator = SpanChunkGenerator.Null;
            // Check for the terminating "}"
            if (!Optional(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.OnError(
                    startingBraceLocation,
                    RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(
                        SyntaxConstants.CSharp.SectionKeyword,
                        Language.GetSample(CSharpSymbolType.RightBrace),
                        Language.GetSample(CSharpSymbolType.LeftBrace)),
                    length: 1 /* } */);
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            CompleteBlock(insertMarkerIfNecessary: false, captureWhitespaceToEndOfLine: true);
            Output(SpanKind.MetaCode);
            return;
        }

        protected virtual void FunctionsDirective()
        {
            // Set the block type
            Context.CurrentBlock.Type = BlockType.Functions;

            // Verify we're on "functions" and accept
            AssertDirective(SyntaxConstants.CSharp.FunctionsKeyword);
            var block = new Block(CurrentSymbol);
            AcceptAndMoveNext();

            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            if (!At(CSharpSymbolType.LeftBrace))
            {
                Context.OnError(
                    CurrentLocation,
                    RazorResources.FormatParseError_Expected_X(Language.GetSample(CSharpSymbolType.LeftBrace)),
                    length: 1 /* { */);
                CompleteBlock();
                Output(SpanKind.MetaCode);
                return;
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            // Capture start point and continue
            var blockStart = CurrentLocation;
            AcceptAndMoveNext();

            // Output what we've seen and continue
            Output(SpanKind.MetaCode);

            var editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            Span.EditHandler = editHandler;

            Balance(BalancingModes.NoErrorOnFailure, CSharpSymbolType.LeftBrace, CSharpSymbolType.RightBrace, blockStart);
            Span.ChunkGenerator = new TypeMemberChunkGenerator();
            if (!At(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.OnError(
                    blockStart,
                    RazorResources.FormatParseError_Expected_EndOfBlock_Before_EOF(block.Name, "}", "{"),
                    length: 1 /* } */);
                CompleteBlock();
                Output(SpanKind.Code);
            }
            else
            {
                Output(SpanKind.Code);
                Assert(CSharpSymbolType.RightBrace);
                Span.ChunkGenerator = SpanChunkGenerator.Null;
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                AcceptAndMoveNext();
                CompleteBlock();
                Output(SpanKind.MetaCode);
            }
        }

        protected virtual void InheritsDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(SyntaxConstants.CSharp.InheritsKeyword);
            AcceptAndMoveNext();

            InheritsDirectiveCore();
        }

        [Conditional("DEBUG")]
        protected void AssertDirective(string directive)
        {
            Assert(CSharpSymbolType.Identifier);
            Debug.Assert(string.Equals(CurrentSymbol.Content, directive, StringComparison.Ordinal));
        }

        protected void InheritsDirectiveCore()
        {
            BaseTypeDirective(
                RazorResources.ParseError_InheritsKeyword_Must_Be_Followed_By_TypeName,
                baseType => new SetBaseTypeChunkGenerator(baseType));
        }

        protected void BaseTypeDirective(string noTypeNameError, Func<string, SpanChunkGenerator> createChunkGenerator)
        {
            var keywordStartLocation = Span.Start;

            // Set the block type
            Context.CurrentBlock.Type = BlockType.Directive;

            var keywordLength = Span.GetContent().Value.Length;

            // Accept whitespace
            var remainingWhitespace = AcceptSingleWhiteSpaceCharacter();

            if (Span.Symbols.Count > 1)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            Output(SpanKind.MetaCode);

            if (remainingWhitespace != null)
            {
                Accept(remainingWhitespace);
            }

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (EndOfFile || At(CSharpSymbolType.WhiteSpace) || At(CSharpSymbolType.NewLine))
            {
                Context.OnError(
                    keywordStartLocation,
                    noTypeNameError,
                    keywordLength);
            }

            // Parse to the end of the line
            AcceptUntil(CSharpSymbolType.NewLine);
            if (!Context.DesignTimeMode)
            {
                // We want the newline to be treated as code, but it causes issues at design-time.
                Optional(CSharpSymbolType.NewLine);
            }

            // Pull out the type name
            string baseType = Span.GetContent();

            // Set up chunk generation
            Span.ChunkGenerator = createChunkGenerator(baseType.Trim());

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code, AcceptedCharacters.AnyExceptNewline);
        }

        private void TagHelperDirective(string keyword, Func<string, ISpanChunkGenerator> chunkGeneratorFactory)
        {
            AssertDirective(keyword);
            var keywordStartLocation = CurrentLocation;

            // Accept the directive name
            AcceptAndMoveNext();

            // Set the block type
            Context.CurrentBlock.Type = BlockType.Directive;

            var keywordLength = Span.GetContent().Value.Length;

            var foundWhitespace = At(CSharpSymbolType.WhiteSpace);
            AcceptWhile(CSharpSymbolType.WhiteSpace);

            // If we found whitespace then any content placed within the whitespace MAY cause a destructive change
            // to the document.  We can't accept it.
            Output(SpanKind.MetaCode, foundWhitespace ? AcceptedCharacters.None : AcceptedCharacters.AnyExceptNewline);

            ISpanChunkGenerator chunkGenerator;
            if (EndOfFile || At(CSharpSymbolType.NewLine))
            {
                Context.OnError(
                    keywordStartLocation,
                    RazorResources.FormatParseError_DirectiveMustHaveValue(keyword),
                    keywordLength);

                chunkGenerator = chunkGeneratorFactory(string.Empty);
            }
            else
            {
                // Need to grab the current location before we accept until the end of the line.
                var startLocation = CurrentLocation;

                // Parse to the end of the line. Essentially accepts anything until end of line, comments, invalid code
                // etc.
                AcceptUntil(CSharpSymbolType.NewLine);

                // Pull out the value and remove whitespaces and optional quotes
                var rawValue = Span.GetContent().Value.Trim();

                var startsWithQuote = rawValue.StartsWith("\"", StringComparison.Ordinal);
                var endsWithQuote = rawValue.EndsWith("\"", StringComparison.Ordinal);
                if (startsWithQuote != endsWithQuote)
                {
                    Context.OnError(
                        startLocation,
                        RazorResources.FormatParseError_IncompleteQuotesAroundDirective(keyword),
                        rawValue.Length);
                }
                else if (startsWithQuote)
                {
                    if (rawValue.Length > 2)
                    {
                        // Remove extra quotes
                        rawValue = rawValue.Substring(1, rawValue.Length - 2);
                    }
                    else
                    {
                        // raw value is only quotes
                        rawValue = string.Empty;
                    }
                }

                chunkGenerator = chunkGeneratorFactory(rawValue);
            }

            Span.ChunkGenerator = chunkGenerator;

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code, AcceptedCharacters.AnyExceptNewline);
        }
    }
}
