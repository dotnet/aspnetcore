// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class CSharpCodeParser
    {
        private void SetupDirectives()
        {
            MapDirectives(InheritsDirective, SyntaxConstants.CSharp.InheritsKeyword);
            MapDirectives(FunctionsDirective, SyntaxConstants.CSharp.FunctionsKeyword);
            MapDirectives(SectionDirective, SyntaxConstants.CSharp.SectionKeyword);
            MapDirectives(HelperDirective, SyntaxConstants.CSharp.HelperKeyword);
            MapDirectives(LayoutDirective, SyntaxConstants.CSharp.LayoutKeyword);
            MapDirectives(SessionStateDirective, SyntaxConstants.CSharp.SessionStateKeyword);
        }

        protected virtual void LayoutDirective()
        {
            AssertDirective(SyntaxConstants.CSharp.LayoutKeyword);
            AcceptAndMoveNext();
            Context.CurrentBlock.Type = BlockType.Directive;

            // Accept spaces, but not newlines
            bool foundSomeWhitespace = At(CSharpSymbolType.WhiteSpace);
            AcceptWhile(CSharpSymbolType.WhiteSpace);
            Output(SpanKind.MetaCode, foundSomeWhitespace ? AcceptedCharacters.None : AcceptedCharacters.Any);

            // First non-whitespace character starts the Layout Page, then newline ends it
            AcceptUntil(CSharpSymbolType.NewLine);
            Span.CodeGenerator = new SetLayoutCodeGenerator(Span.GetContent());
            Span.EditHandler.EditorHints = EditorHints.LayoutPage | EditorHints.VirtualPath;
            bool foundNewline = Optional(CSharpSymbolType.NewLine);
            AddMarkerSymbolIfNecessary();
            Output(SpanKind.MetaCode, foundNewline ? AcceptedCharacters.None : AcceptedCharacters.Any);
        }

        protected virtual void SessionStateDirective()
        {
            AssertDirective(SyntaxConstants.CSharp.SessionStateKeyword);
            AcceptAndMoveNext();

            SessionStateDirectiveCore();
        }

        protected void SessionStateDirectiveCore()
        {
            SessionStateTypeDirective(RazorResources.ParserEror_SessionDirectiveMissingValue, (key, value) => new RazorDirectiveAttributeCodeGenerator(key, value));
        }

        protected void SessionStateTypeDirective(string noValueError, Func<string, string, SpanCodeGenerator> createCodeGenerator)
        {
            // Set the block type
            Context.CurrentBlock.Type = BlockType.Directive;

            // Accept whitespace
            CSharpSymbol remainingWs = AcceptSingleWhiteSpaceCharacter();

            if (Span.Symbols.Count > 1)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            Output(SpanKind.MetaCode);

            if (remainingWs != null)
            {
                Accept(remainingWs);
            }
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            // Parse a Type Name
            if (!ValidSessionStateValue())
            {
                Context.OnError(CurrentLocation, noValueError);
            }

            // Pull out the type name
            string sessionStateValue = String.Concat(
                Span.Symbols
                    .Cast<CSharpSymbol>()
                    .Select(sym => sym.Content)).Trim();

            // Set up code generation
            Span.CodeGenerator = createCodeGenerator(SyntaxConstants.CSharp.SessionStateKeyword, sessionStateValue);

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code);
        }

        protected virtual bool ValidSessionStateValue()
        {
            return Optional(CSharpSymbolType.Identifier);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Coupling will be reviewed at a later date")]
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "C# Keywords are always lower-case")]
        protected virtual void HelperDirective()
        {
            bool nested = Context.IsWithin(BlockType.Helper);

            // Set the block and span type
            Context.CurrentBlock.Type = BlockType.Helper;

            // Verify we're on "helper" and accept
            AssertDirective(SyntaxConstants.CSharp.HelperKeyword);
            Block block = new Block(CurrentSymbol.Content.ToString().ToLowerInvariant(), CurrentLocation);
            AcceptAndMoveNext();

            if (nested)
            {
                Context.OnError(CurrentLocation, RazorResources.ParseError_Helpers_Cannot_Be_Nested);
            }

            // Accept a single whitespace character if present, if not, we should stop now
            if (!At(CSharpSymbolType.WhiteSpace))
            {
                string error;
                if (At(CSharpSymbolType.NewLine))
                {
                    error = RazorResources.ErrorComponent_Newline;
                }
                else if (EndOfFile)
                {
                    error = RazorResources.ErrorComponent_EndOfFile;
                }
                else
                {
                    error = RazorResources.ErrorComponent_Character(CurrentSymbol.Content);
                }

                Context.OnError(
                    CurrentLocation,
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start(error));
                PutCurrentBack();
                Output(SpanKind.MetaCode);
                return;
            }

            CSharpSymbol remainingWs = AcceptSingleWhiteSpaceCharacter();

            // Output metacode and continue
            Output(SpanKind.MetaCode);
            if (remainingWs != null)
            {
                Accept(remainingWs);
            }
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true)); // Don't accept newlines.

            // Expecting an identifier (helper name)
            bool errorReported = !Required(CSharpSymbolType.Identifier, errorIfNotFound: true, errorBase: RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start);
            if (!errorReported)
            {
                Assert(CSharpSymbolType.Identifier);
                AcceptAndMoveNext();
            }

            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            // Expecting parameter list start: "("
            SourceLocation bracketErrorPos = CurrentLocation;
            if (!Optional(CSharpSymbolType.LeftParenthesis))
            {
                if (!errorReported)
                {
                    errorReported = true;
                    Context.OnError(
                        CurrentLocation,
                        RazorResources.ParseError_MissingCharAfterHelperName("("));
                }
            }
            else
            {
                SourceLocation bracketStart = CurrentLocation;
                if (!Balance(BalancingModes.NoErrorOnFailure,
                             CSharpSymbolType.LeftParenthesis,
                             CSharpSymbolType.RightParenthesis,
                             bracketStart))
                {
                    errorReported = true;
                    Context.OnError(
                        bracketErrorPos,
                        RazorResources.ParseError_UnterminatedHelperParameterList);
                }
                Optional(CSharpSymbolType.RightParenthesis);
            }

            int bookmark = CurrentLocation.AbsoluteIndex;
            IEnumerable<CSharpSymbol> ws = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: true));

            // Expecting a "{"
            SourceLocation errorLocation = CurrentLocation;
            bool headerComplete = At(CSharpSymbolType.LeftBrace);
            if (headerComplete)
            {
                Accept(ws);
                AcceptAndMoveNext();
            }
            else
            {
                Context.Source.Position = bookmark;
                NextToken();
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));
                if (!errorReported)
                {
                    Context.OnError(
                        errorLocation,
                        RazorResources.ParseError_MissingCharAfterHelperParameters(
                            Language.GetSample(CSharpSymbolType.LeftBrace)));
                }
            }

            // Grab the signature and build the code generator
            AddMarkerSymbolIfNecessary();
            LocationTagged<string> signature = Span.GetContent();
            HelperCodeGenerator blockGen = new HelperCodeGenerator(signature, headerComplete);
            Context.CurrentBlock.CodeGenerator = blockGen;

            // The block will generate appropriate code, 
            Span.CodeGenerator = SpanCodeGenerator.Null;

            if (!headerComplete)
            {
                CompleteBlock();
                Output(SpanKind.Code);
                return;
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                Output(SpanKind.Code);
            }

            // We're valid, so parse the nested block
            AutoCompleteEditHandler bodyEditHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            using (PushSpanConfig(DefaultSpanConfig))
            {
                using (Context.StartBlock(BlockType.Statement))
                {
                    Span.EditHandler = bodyEditHandler;
                    CodeBlock(false, block);
                    CompleteBlock(insertMarkerIfNecessary: true);
                    Output(SpanKind.Code);
                }
            }
            Initialize(Span);

            EnsureCurrent();

            Span.CodeGenerator = SpanCodeGenerator.Null; // The block will generate the footer code.
            if (!Optional(CSharpSymbolType.RightBrace))
            {
                // The } is missing, so set the initial signature span to use it as an autocomplete string
                bodyEditHandler.AutoCompleteString = "}";

                // Need to be able to accept anything to properly handle the autocomplete
                bodyEditHandler.AcceptedCharacters = AcceptedCharacters.Any;
            }
            else
            {
                blockGen.Footer = Span.GetContent();
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            CompleteBlock();
            Output(SpanKind.Code);
        }

        protected virtual void SectionDirective()
        {
            bool nested = Context.IsWithin(BlockType.Section);
            bool errorReported = false;

            // Set the block and span type
            Context.CurrentBlock.Type = BlockType.Section;

            // Verify we're on "section" and accept
            AssertDirective(SyntaxConstants.CSharp.SectionKeyword);
            AcceptAndMoveNext();

            if (nested)
            {
                Context.OnError(CurrentLocation, RazorResources.ParseError_Sections_Cannot_Be_Nested(RazorResources.SectionExample_CS));
                errorReported = true;
            }

            IEnumerable<CSharpSymbol> ws = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            // Get the section name
            string sectionName = String.Empty;
            if (!Required(CSharpSymbolType.Identifier,
                          errorIfNotFound: true,
                          errorBase: RazorResources.ParseError_Unexpected_Character_At_Section_Name_Start))
            {
                if (!errorReported)
                {
                    errorReported = true;
                }

                PutCurrentBack();
                PutBack(ws);
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: false));
            }
            else
            {
                Accept(ws);
                sectionName = CurrentSymbol.Content;
                AcceptAndMoveNext();
            }
            Context.CurrentBlock.CodeGenerator = new SectionCodeGenerator(sectionName);

            SourceLocation errorLocation = CurrentLocation;
            ws = ReadWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            // Get the starting brace
            bool sawStartingBrace = At(CSharpSymbolType.LeftBrace);
            if (!sawStartingBrace)
            {
                if (!errorReported)
                {
                    errorReported = true;
                    Context.OnError(errorLocation, RazorResources.ParseError_MissingOpenBraceAfterSection);
                }

                PutCurrentBack();
                PutBack(ws);
                AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: false));
                Optional(CSharpSymbolType.NewLine);
                Output(SpanKind.MetaCode);
                CompleteBlock();
                return;
            }
            else
            {
                Accept(ws);
            }

            // Set up edit handler
            AutoCompleteEditHandler editHandler = new AutoCompleteEditHandler(Language.TokenizeString) { AutoCompleteAtEndOfSpan = true };

            Span.EditHandler = editHandler;
            Span.Accept(CurrentSymbol);

            // Output Metacode then switch to section parser
            Output(SpanKind.MetaCode);
            SectionBlock("{", "}", caseSensitive: true);

            Span.CodeGenerator = SpanCodeGenerator.Null;
            // Check for the terminating "}"
            if (!Optional(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.OnError(CurrentLocation,
                                RazorResources.ParseError_Expected_X(
                                    Language.GetSample(CSharpSymbolType.RightBrace)));
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
            Block block = new Block(CurrentSymbol);
            AcceptAndMoveNext();

            AcceptWhile(IsSpacingToken(includeNewLines: true, includeComments: false));

            if (!At(CSharpSymbolType.LeftBrace))
            {
                Context.OnError(CurrentLocation,
                                RazorResources.ParseError_Expected_X(Language.GetSample(CSharpSymbolType.LeftBrace)));
                CompleteBlock();
                Output(SpanKind.MetaCode);
                return;
            }
            else
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            // Capture start point and continue
            SourceLocation blockStart = CurrentLocation;
            AcceptAndMoveNext();

            // Output what we've seen and continue
            Output(SpanKind.MetaCode);

            AutoCompleteEditHandler editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            Span.EditHandler = editHandler;

            Balance(BalancingModes.NoErrorOnFailure, CSharpSymbolType.LeftBrace, CSharpSymbolType.RightBrace, blockStart);
            Span.CodeGenerator = new TypeMemberCodeGenerator();
            if (!At(CSharpSymbolType.RightBrace))
            {
                editHandler.AutoCompleteString = "}";
                Context.OnError(block.Start, RazorResources.ParseError_Expected_EndOfBlock_Before_EOF(block.Name, "}", "{"));
                CompleteBlock();
                Output(SpanKind.Code);
            }
            else
            {
                Output(SpanKind.Code);
                Assert(CSharpSymbolType.RightBrace);
                Span.CodeGenerator = SpanCodeGenerator.Null;
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

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "directive", Justification = "This only occurs in Release builds, where this method is empty by design")]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This only occurs in Release builds, where this method is empty by design")]
        [Conditional("DEBUG")]
        protected void AssertDirective(string directive)
        {
            Assert(CSharpSymbolType.Identifier);
            Debug.Assert(String.Equals(CurrentSymbol.Content, directive, StringComparison.Ordinal));
        }

        protected void InheritsDirectiveCore()
        {
            BaseTypeDirective(RazorResources.ParseError_InheritsKeyword_Must_Be_Followed_By_TypeName, baseType => new SetBaseTypeCodeGenerator(baseType));
        }

        protected void BaseTypeDirective(string noTypeNameError, Func<string, SpanCodeGenerator> createCodeGenerator)
        {
            // Set the block type
            Context.CurrentBlock.Type = BlockType.Directive;

            // Accept whitespace
            CSharpSymbol remainingWs = AcceptSingleWhiteSpaceCharacter();

            if (Span.Symbols.Count > 1)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            Output(SpanKind.MetaCode);

            if (remainingWs != null)
            {
                Accept(remainingWs);
            }
            AcceptWhile(IsSpacingToken(includeNewLines: false, includeComments: true));

            if (EndOfFile || At(CSharpSymbolType.WhiteSpace) || At(CSharpSymbolType.NewLine))
            {
                Context.OnError(CurrentLocation, noTypeNameError);
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

            // Set up code generation
            Span.CodeGenerator = createCodeGenerator(baseType.Trim());

            // Output the span and finish the block
            CompleteBlock();
            Output(SpanKind.Code);
        }
    }
}
