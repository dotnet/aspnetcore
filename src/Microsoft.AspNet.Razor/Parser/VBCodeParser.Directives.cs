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
using Microsoft.AspNet.Razor.Resources;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class VBCodeParser : TokenizerBackedParser<VBTokenizer, VBSymbol, VBSymbolType>
    {
        private void SetUpDirectives()
        {
            MapDirective(SyntaxConstants.VB.CodeKeyword, EndTerminatedDirective(SyntaxConstants.VB.CodeKeyword,
                                                                                BlockType.Statement,
                                                                                new StatementCodeGenerator(),
                                                                                allowMarkup: true));
            MapDirective(SyntaxConstants.VB.FunctionsKeyword, EndTerminatedDirective(SyntaxConstants.VB.FunctionsKeyword,
                                                                                     BlockType.Functions,
                                                                                     new TypeMemberCodeGenerator(),
                                                                                     allowMarkup: false));
            MapDirective(SyntaxConstants.VB.SectionKeyword, SectionDirective);
            MapDirective(SyntaxConstants.VB.HelperKeyword, HelperDirective);

            MapDirective(SyntaxConstants.VB.LayoutKeyword, LayoutDirective);
            MapDirective(SyntaxConstants.VB.SessionStateKeyword, SessionStateDirective);
        }

        protected virtual bool LayoutDirective()
        {
            AssertDirective(SyntaxConstants.VB.LayoutKeyword);
            AcceptAndMoveNext();
            Context.CurrentBlock.Type = BlockType.Directive;

            // Accept spaces, but not newlines
            bool foundSomeWhitespace = At(VBSymbolType.WhiteSpace);
            AcceptWhile(VBSymbolType.WhiteSpace);
            Output(SpanKind.MetaCode, foundSomeWhitespace ? AcceptedCharacters.None : AcceptedCharacters.Any);

            // First non-whitespace character starts the Layout Page, then newline ends it
            AcceptUntil(VBSymbolType.NewLine);
            Span.CodeGenerator = new SetLayoutCodeGenerator(Span.GetContent());
            Span.EditHandler.EditorHints = EditorHints.LayoutPage | EditorHints.VirtualPath;
            bool foundNewline = Optional(VBSymbolType.NewLine);
            AddMarkerSymbolIfNecessary();
            Output(SpanKind.MetaCode, foundNewline ? AcceptedCharacters.None : AcceptedCharacters.Any);
            return true;
        }

        protected virtual bool SessionStateDirective()
        {
            AssertDirective(SyntaxConstants.VB.SessionStateKeyword);
            AcceptAndMoveNext();
            Context.CurrentBlock.Type = BlockType.Directive;

            // Accept spaces, but not newlines
            bool foundSomeWhitespace = At(VBSymbolType.WhiteSpace);
            AcceptWhile(VBSymbolType.WhiteSpace);
            Output(SpanKind.MetaCode, foundSomeWhitespace ? AcceptedCharacters.None : AcceptedCharacters.Any);

            // First non-whitespace character starts the session state directive, then newline ends it
            AcceptUntil(VBSymbolType.NewLine);
            var value = String.Concat(Span.Symbols.Select(sym => sym.Content));
            Span.CodeGenerator = new RazorDirectiveAttributeCodeGenerator(SyntaxConstants.VB.SessionStateKeyword, value);
            bool foundNewline = Optional(VBSymbolType.NewLine);
            AddMarkerSymbolIfNecessary();
            Output(SpanKind.MetaCode, foundNewline ? AcceptedCharacters.None : AcceptedCharacters.Any);
            return true;
        }

        protected virtual bool HelperDirective()
        {
            if (Context.IsWithin(BlockType.Helper))
            {
                Context.OnError(CurrentLocation, RazorResources.ParseError_Helpers_Cannot_Be_Nested);
            }

            Context.CurrentBlock.Type = BlockType.Helper;
            SourceLocation blockStart = CurrentLocation;

            AssertDirective(SyntaxConstants.VB.HelperKeyword);
            AcceptAndMoveNext();

            VBSymbolType firstAfterKeyword = VBSymbolType.Unknown;
            if (CurrentSymbol != null)
            {
                firstAfterKeyword = CurrentSymbol.Type;
            }

            VBSymbol remainingWs = null;
            if (At(VBSymbolType.NewLine))
            {
                // Accept a _single_ new line, we'll be aborting later.
                AcceptAndMoveNext();
            }
            else
            {
                remainingWs = AcceptSingleWhiteSpaceCharacter();
            }
            if (firstAfterKeyword == VBSymbolType.WhiteSpace || firstAfterKeyword == VBSymbolType.NewLine)
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }
            Output(SpanKind.MetaCode);
            if (firstAfterKeyword != VBSymbolType.WhiteSpace)
            {
                string error;
                if (At(VBSymbolType.NewLine))
                {
                    error = RazorResources.ErrorComponent_Newline;
                }
                else if (EndOfFile)
                {
                    error = RazorResources.ErrorComponent_EndOfFile;
                }
                else
                {
                    error = String.Format(CultureInfo.CurrentCulture, RazorResources.ErrorComponent_Character, CurrentSymbol.Content);
                }

                Context.OnError(
                    CurrentLocation,
                    RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start,
                    error);

                // Bail out.
                PutCurrentBack();
                Output(SpanKind.Code);
                return false;
            }

            if (remainingWs != null)
            {
                Accept(remainingWs);
            }

            bool errorReported = !Required(VBSymbolType.Identifier, RazorResources.ParseError_Unexpected_Character_At_Helper_Name_Start);

            AcceptWhile(VBSymbolType.WhiteSpace);

            SourceLocation parensStart = CurrentLocation;
            bool headerComplete = false;
            if (!Optional(VBSymbolType.LeftParenthesis))
            {
                if (!errorReported)
                {
                    errorReported = true;
                    Context.OnError(CurrentLocation,
                                    RazorResources.ParseError_MissingCharAfterHelperName,
                                    VBSymbol.GetSample(VBSymbolType.LeftParenthesis));
                }
            }
            else if (!Balance(BalancingModes.NoErrorOnFailure, VBSymbolType.LeftParenthesis, VBSymbolType.RightParenthesis, parensStart))
            {
                Context.OnError(parensStart, RazorResources.ParseError_UnterminatedHelperParameterList);
            }
            else
            {
                Expected(VBSymbolType.RightParenthesis);
                headerComplete = true;
            }

            AddMarkerSymbolIfNecessary();
            Context.CurrentBlock.CodeGenerator = new HelperCodeGenerator(
                Span.GetContent(),
                headerComplete);
            AutoCompleteEditHandler editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            Span.EditHandler = editHandler;
            Output(SpanKind.Code);

            if (headerComplete)
            {
                bool old = IsNested;
                IsNested = true;
                using (Context.StartBlock(BlockType.Statement))
                {
                    using (PushSpanConfig(StatementBlockSpanConfiguration(new StatementCodeGenerator())))
                    {
                        try
                        {
                            if (!EndTerminatedDirectiveBody(SyntaxConstants.VB.HelperKeyword, blockStart, allowAllTransitions: true))
                            {
                                if (Context.LastAcceptedCharacters != AcceptedCharacters.Any)
                                {
                                    AddMarkerSymbolIfNecessary();
                                }

                                editHandler.AutoCompleteString = SyntaxConstants.VB.EndHelperKeyword;
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        finally
                        {
                            Output(SpanKind.Code);
                            IsNested = old;
                        }
                    }
                }
            }
            else
            {
                Output(SpanKind.Code);
            }
            PutCurrentBack();
            return false;
        }

        protected virtual bool SectionDirective()
        {
            SourceLocation start = CurrentLocation;
            AssertDirective(SyntaxConstants.VB.SectionKeyword);
            AcceptAndMoveNext();

            if (Context.IsWithin(BlockType.Section))
            {
                Context.OnError(CurrentLocation, RazorResources.ParseError_Sections_Cannot_Be_Nested, RazorResources.SectionExample_VB);
            }

            if (At(VBSymbolType.NewLine))
            {
                AcceptAndMoveNext();
            }
            else
            {
                AcceptVBSpaces();
            }
            string sectionName = null;
            if (!At(VBSymbolType.Identifier))
            {
                Context.OnError(CurrentLocation,
                                RazorResources.ParseError_Unexpected_Character_At_Section_Name_Start,
                                GetCurrentSymbolDisplay());
            }
            else
            {
                sectionName = CurrentSymbol.Content;
                AcceptAndMoveNext();
            }
            Context.CurrentBlock.Type = BlockType.Section;
            Context.CurrentBlock.CodeGenerator = new SectionCodeGenerator(sectionName ?? String.Empty);

            AutoCompleteEditHandler editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
            Span.EditHandler = editHandler;

            PutCurrentBack();

            Output(SpanKind.MetaCode);

            // Parse the section
            OtherParserBlock(null, SyntaxConstants.VB.EndSectionKeyword);

            Span.CodeGenerator = SpanCodeGenerator.Null;
            bool complete = false;
            if (!At(VBKeyword.End))
            {
                Context.OnError(start,
                                RazorResources.ParseError_BlockNotTerminated,
                                SyntaxConstants.VB.SectionKeyword,
                                SyntaxConstants.VB.EndSectionKeyword);
                editHandler.AutoCompleteString = SyntaxConstants.VB.EndSectionKeyword;
            }
            else
            {
                AcceptAndMoveNext();
                AcceptWhile(VBSymbolType.WhiteSpace);
                if (!At(SyntaxConstants.VB.SectionKeyword))
                {
                    Context.OnError(start,
                                    RazorResources.ParseError_BlockNotTerminated,
                                    SyntaxConstants.VB.SectionKeyword,
                                    SyntaxConstants.VB.EndSectionKeyword);
                }
                else
                {
                    AcceptAndMoveNext();
                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                    complete = true;
                }
            }
            PutCurrentBack();
            Output(SpanKind.MetaCode);
            return complete;
        }

        protected virtual Func<bool> EndTerminatedDirective(string directive, BlockType blockType, SpanCodeGenerator codeGenerator, bool allowMarkup)
        {
            return () =>
            {
                SourceLocation blockStart = CurrentLocation;
                Context.CurrentBlock.Type = blockType;
                AssertDirective(directive);
                AcceptAndMoveNext();

                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                Span.CodeGenerator = SpanCodeGenerator.Null;
                Output(SpanKind.MetaCode);

                using (PushSpanConfig(StatementBlockSpanConfiguration(codeGenerator)))
                {
                    AutoCompleteEditHandler editHandler = new AutoCompleteEditHandler(Language.TokenizeString);
                    Span.EditHandler = editHandler;

                    if (!EndTerminatedDirectiveBody(directive, blockStart, allowMarkup))
                    {
                        editHandler.AutoCompleteString = String.Concat(SyntaxConstants.VB.EndKeyword, " ", directive);
                        return false;
                    }
                    return true;
                }
            };
        }

        protected virtual bool EndTerminatedDirectiveBody(string directive, SourceLocation blockStart, bool allowAllTransitions)
        {
            while (!EndOfFile)
            {
                VBSymbol lastWhitespace = AcceptWhiteSpaceInLines();
                if (IsAtEmbeddedTransition(allowTemplatesAndComments: allowAllTransitions, allowTransitions: allowAllTransitions))
                {
                    HandleEmbeddedTransition(lastWhitespace);
                }
                else
                {
                    if (At(VBKeyword.End))
                    {
                        Accept(lastWhitespace);
                        VBSymbol end = CurrentSymbol;
                        NextToken();
                        IEnumerable<VBSymbol> ws = ReadVBSpaces();
                        if (At(directive))
                        {
                            if (Context.LastAcceptedCharacters != AcceptedCharacters.Any)
                            {
                                AddMarkerSymbolIfNecessary(end.Start);
                            }
                            Output(SpanKind.Code);
                            Accept(end);
                            Accept(ws);
                            AcceptAndMoveNext();
                            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                            Span.CodeGenerator = SpanCodeGenerator.Null;
                            Output(SpanKind.MetaCode);
                            return true;
                        }
                        else
                        {
                            Accept(end);
                            Accept(ws);
                            AcceptAndMoveNext();
                        }
                    }
                    else
                    {
                        Accept(lastWhitespace);
                        AcceptAndMoveNext();
                    }
                }
            }

            // This is a language keyword, so it does not need to be localized
            Context.OnError(blockStart, RazorResources.ParseError_BlockNotTerminated, directive, String.Concat(SyntaxConstants.VB.EndKeyword, " ", directive));
            return false;
        }

        protected bool At(string directive)
        {
            return At(VBSymbolType.Identifier) && String.Equals(CurrentSymbol.Content, directive, StringComparison.OrdinalIgnoreCase);
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "'this' is used in DEBUG builds")]
        [Conditional("DEBUG")]
        protected void AssertDirective(string directive)
        {
            Assert(VBSymbolType.Identifier);
            Debug.Assert(String.Equals(directive, CurrentSymbol.Content, StringComparison.OrdinalIgnoreCase));
        }

        private string GetCurrentSymbolDisplay()
        {
            if (EndOfFile)
            {
                return RazorResources.ErrorComponent_EndOfFile;
            }
            else if (At(VBSymbolType.NewLine))
            {
                return RazorResources.ErrorComponent_Newline;
            }
            else if (At(VBSymbolType.WhiteSpace))
            {
                return RazorResources.ErrorComponent_Whitespace;
            }
            else
            {
                return String.Format(CultureInfo.CurrentCulture, RazorResources.ErrorComponent_Character, CurrentSymbol.Content);
            }
        }
    }
}
