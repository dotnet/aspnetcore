// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
        private void SetUpKeywords()
        {
            MapKeyword(VBKeyword.Using, EndTerminatedStatement(VBKeyword.Using, supportsExit: false, supportsContinue: false)); // http://msdn.microsoft.com/en-us/library/htd05whh.aspx
            MapKeyword(VBKeyword.While, EndTerminatedStatement(VBKeyword.While, supportsExit: true, supportsContinue: true)); // http://msdn.microsoft.com/en-us/library/zh1f56zs.aspx
            MapKeyword(VBKeyword.If, EndTerminatedStatement(VBKeyword.If, supportsExit: false, supportsContinue: false)); // http://msdn.microsoft.com/en-us/library/752y8abs.aspx
            MapKeyword(VBKeyword.Select, EndTerminatedStatement(VBKeyword.Select, supportsExit: true, supportsContinue: false, blockName: SyntaxConstants.VB.SelectCaseKeyword)); // http://msdn.microsoft.com/en-us/library/cy37t14y.aspx
            MapKeyword(VBKeyword.Try, EndTerminatedStatement(VBKeyword.Try, supportsExit: true, supportsContinue: false)); // http://msdn.microsoft.com/en-us/library/fk6t46tz.aspx
            MapKeyword(VBKeyword.With, EndTerminatedStatement(VBKeyword.With, supportsExit: false, supportsContinue: false)); // http://msdn.microsoft.com/en-us/library/wc500chb.aspx
            MapKeyword(VBKeyword.SyncLock, EndTerminatedStatement(VBKeyword.SyncLock, supportsExit: false, supportsContinue: false)); // http://msdn.microsoft.com/en-us/library/3a86s51t.aspx

            // http://msdn.microsoft.com/en-us/library/5z06z1kb.aspx
            // http://msdn.microsoft.com/en-us/library/5ebk1751.aspx
            MapKeyword(VBKeyword.For, KeywordTerminatedStatement(VBKeyword.For, VBKeyword.Next, supportsExit: true, supportsContinue: true));
            MapKeyword(VBKeyword.Do, KeywordTerminatedStatement(VBKeyword.Do, VBKeyword.Loop, supportsExit: true, supportsContinue: true)); // http://msdn.microsoft.com/en-us/library/eked04a7.aspx

            MapKeyword(VBKeyword.Imports, ImportsStatement);
            MapKeyword(VBKeyword.Option, OptionStatement);
            MapKeyword(VBKeyword.Inherits, InheritsStatement);

            MapKeyword(VBKeyword.Class, ReservedWord);
            MapKeyword(VBKeyword.Namespace, ReservedWord);
        }

        protected virtual bool InheritsStatement()
        {
            Assert(VBKeyword.Inherits);

            Span.CodeGenerator = SpanCodeGenerator.Null;
            Context.CurrentBlock.Type = BlockType.Directive;

            AcceptAndMoveNext();
            SourceLocation endInherits = CurrentLocation;

            if (At(VBSymbolType.WhiteSpace))
            {
                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            }

            AcceptWhile(VBSymbolType.WhiteSpace);
            Output(SpanKind.MetaCode);

            if (EndOfFile || At(VBSymbolType.WhiteSpace) || At(VBSymbolType.NewLine))
            {
                Context.OnError(endInherits, RazorResources.ParseError_InheritsKeyword_Must_Be_Followed_By_TypeName);
            }

            // Just accept to a newline
            AcceptUntil(VBSymbolType.NewLine);
            if (!Context.DesignTimeMode)
            {
                // We want the newline to be treated as code, but it causes issues at design-time.
                Optional(VBSymbolType.NewLine);
            }

            string baseType = Span.GetContent();
            Span.CodeGenerator = new SetBaseTypeCodeGenerator(baseType.Trim());

            Output(SpanKind.Code);
            return false;
        }

        protected virtual bool OptionStatement()
        {
            try
            {
                Context.CurrentBlock.Type = BlockType.Directive;

                Assert(VBKeyword.Option);
                AcceptAndMoveNext();
                AcceptWhile(VBSymbolType.WhiteSpace);
                if (!At(VBSymbolType.Identifier))
                {
                    if (CurrentSymbol != null)
                    {
                        Context.OnError(CurrentLocation, String.Format(CultureInfo.CurrentCulture,
                                                                       RazorResources.ParseError_Unexpected,
                                                                       CurrentSymbol.Content));
                    }
                    return false;
                }
                SourceLocation optionLoc = CurrentLocation;
                string option = CurrentSymbol.Content;
                AcceptAndMoveNext();

                AcceptWhile(VBSymbolType.WhiteSpace);
                bool boolVal;
                if (At(VBKeyword.On))
                {
                    AcceptAndMoveNext();
                    boolVal = true;
                }
                else if (At(VBSymbolType.Identifier))
                {
                    if (String.Equals(CurrentSymbol.Content, SyntaxConstants.VB.OffKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        AcceptAndMoveNext();
                        boolVal = false;
                    }
                    else
                    {
                        Context.OnError(CurrentLocation, String.Format(CultureInfo.CurrentCulture,
                                                                       RazorResources.ParseError_InvalidOptionValue,
                                                                       option,
                                                                       CurrentSymbol.Content));
                        AcceptAndMoveNext();
                        return false;
                    }
                }
                else
                {
                    if (!EndOfFile)
                    {
                        Context.OnError(CurrentLocation, String.Format(CultureInfo.CurrentCulture,
                                                                       RazorResources.ParseError_Unexpected,
                                                                       CurrentSymbol.Content));
                        AcceptAndMoveNext();
                    }
                    return false;
                }

                if (String.Equals(option, SyntaxConstants.VB.StrictKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    Span.CodeGenerator = SetVBOptionCodeGenerator.Strict(boolVal);
                }
                else if (String.Equals(option, SyntaxConstants.VB.ExplicitKeyword, StringComparison.OrdinalIgnoreCase))
                {
                    Span.CodeGenerator = SetVBOptionCodeGenerator.Explicit(boolVal);
                }
                else
                {
                    Span.CodeGenerator = new SetVBOptionCodeGenerator(option, boolVal);
                    Context.OnError(optionLoc, RazorResources.ParseError_UnknownOption, option);
                }
            }
            finally
            {
                if (Span.Symbols.Count > 0)
                {
                    Output(SpanKind.MetaCode);
                }
            }
            return true;
        }

        protected virtual bool ImportsStatement()
        {
            Context.CurrentBlock.Type = BlockType.Directive;
            Assert(VBKeyword.Imports);
            AcceptAndMoveNext();

            AcceptVBSpaces();
            if (At(VBSymbolType.WhiteSpace) || At(VBSymbolType.NewLine))
            {
                Context.OnError(CurrentLocation, RazorResources.ParseError_NamespaceOrTypeAliasExpected);
            }

            // Just accept to a newline
            AcceptUntil(VBSymbolType.NewLine);
            Optional(VBSymbolType.NewLine);

            string ns = String.Concat(Span.Symbols.Skip(1).Select(s => s.Content));
            Span.CodeGenerator = new AddImportCodeGenerator(ns, SyntaxConstants.VB.ImportsKeywordLength);

            Output(SpanKind.MetaCode);
            return false;
        }

        protected virtual Func<bool> EndTerminatedStatement(VBKeyword keyword, bool supportsExit, bool supportsContinue)
        {
            return EndTerminatedStatement(keyword, supportsExit, supportsContinue, blockName: keyword.ToString());
        }

        protected virtual Func<bool> EndTerminatedStatement(VBKeyword keyword, bool supportsExit, bool supportsContinue, string blockName)
        {
            return () =>
            {
                using (PushSpanConfig(StatementBlockSpanConfiguration(new StatementCodeGenerator())))
                {
                    SourceLocation blockStart = CurrentLocation;
                    Assert(keyword);
                    AcceptAndMoveNext();

                    while (!EndOfFile)
                    {
                        VBSymbol lastWhitespace = AcceptWhiteSpaceInLines();
                        if (IsAtEmbeddedTransition(allowTemplatesAndComments: true, allowTransitions: true))
                        {
                            HandleEmbeddedTransition(lastWhitespace);
                        }
                        else
                        {
                            Accept(lastWhitespace);

                            if ((supportsExit && At(VBKeyword.Exit)) || (supportsContinue && At(VBKeyword.Continue)))
                            {
                                HandleExitOrContinue(keyword);
                            }
                            else if (At(VBKeyword.End))
                            {
                                AcceptAndMoveNext();
                                AcceptVBSpaces();
                                if (At(keyword))
                                {
                                    AcceptAndMoveNext();
                                    if (!Context.DesignTimeMode)
                                    {
                                        Optional(VBSymbolType.NewLine);
                                    }
                                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                                    return false;
                                }
                            }
                            else if (At(keyword))
                            {
                                // Parse nested statement
                                EndTerminatedStatement(keyword, supportsExit, supportsContinue)();
                            }
                            else if (!EndOfFile)
                            {
                                AcceptAndMoveNext();
                            }
                        }
                    }

                    Context.OnError(blockStart,
                                    RazorResources.ParseError_BlockNotTerminated,
                                    blockName,
                                    // This is a language keyword, so it does not need to be localized
                                    String.Concat(VBKeyword.End, " ", keyword));
                    return false;
                }
            };
        }

        protected virtual Func<bool> KeywordTerminatedStatement(VBKeyword start, VBKeyword terminator, bool supportsExit, bool supportsContinue)
        {
            return () =>
            {
                using (PushSpanConfig(StatementBlockSpanConfiguration(new StatementCodeGenerator())))
                {
                    SourceLocation blockStart = CurrentLocation;
                    Assert(start);
                    AcceptAndMoveNext();
                    while (!EndOfFile)
                    {
                        VBSymbol lastWhitespace = AcceptWhiteSpaceInLines();
                        if (IsAtEmbeddedTransition(allowTemplatesAndComments: true, allowTransitions: true))
                        {
                            HandleEmbeddedTransition(lastWhitespace);
                        }
                        else
                        {
                            Accept(lastWhitespace);
                            if ((supportsExit && At(VBKeyword.Exit)) || (supportsContinue && At(VBKeyword.Continue)))
                            {
                                HandleExitOrContinue(start);
                            }
                            else if (At(start))
                            {
                                // Parse nested statement
                                KeywordTerminatedStatement(start, terminator, supportsExit, supportsContinue)();
                            }
                            else if (At(terminator))
                            {
                                AcceptUntil(VBSymbolType.NewLine);
                                Optional(VBSymbolType.NewLine);
                                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.AnyExceptNewline;
                                return false;
                            }
                            else if (!EndOfFile)
                            {
                                AcceptAndMoveNext();
                            }
                        }
                    }

                    Context.OnError(blockStart,
                                    RazorResources.ParseError_BlockNotTerminated,
                                    start, terminator);
                    return false;
                }
            };
        }

        protected void HandleExitOrContinue(VBKeyword keyword)
        {
            Assert(VBSymbolType.Keyword);
            Debug.Assert(CurrentSymbol.Keyword == VBKeyword.Continue || CurrentSymbol.Keyword == VBKeyword.Exit);

            // Accept, read whitespace and look for the next keyword
            AcceptAndMoveNext();
            AcceptWhile(VBSymbolType.WhiteSpace);

            // If this is the start keyword, skip it and continue (to avoid starting a nested statement block)
            Optional(keyword);
        }
    }
}
