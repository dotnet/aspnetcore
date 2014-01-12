// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        internal static ISet<string> DefaultKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "functions",
            "code",
            "section",
            "do",
            "while",
            "if",
            "select",
            "for",
            "try",
            "with",
            "synclock",
            "using",
            "imports",
            "inherits",
            "option",
            "helper",
            "namespace",
            "class",
            "layout",
            "sessionstate"
        };

        private Dictionary<VBKeyword, Func<bool>> _keywordHandlers = new Dictionary<VBKeyword, Func<bool>>();
        private Dictionary<string, Func<bool>> _directiveHandlers = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase);

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Necessary state is initialized before calling virtual methods")]
        public VBCodeParser()
        {
            DirectParentIsCode = false;
            Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SetUpKeywords();
            SetUpDirectives();
        }

        protected internal ISet<string> Keywords { get; private set; }

        protected override LanguageCharacteristics<VBTokenizer, VBSymbol, VBSymbolType> Language
        {
            get { return VBLanguageCharacteristics.Instance; }
        }

        protected override ParserBase OtherParser
        {
            get { return Context.MarkupParser; }
        }

        private bool IsNested { get; set; }
        private bool DirectParentIsCode { get; set; }

        protected override bool IsAtEmbeddedTransition(bool allowTemplatesAndComments, bool allowTransitions)
        {
            return (allowTransitions && Language.IsTransition(CurrentSymbol) && !Was(VBSymbolType.Dot)) ||
                   (allowTemplatesAndComments && Language.IsCommentStart(CurrentSymbol)) ||
                   (Language.IsTransition(CurrentSymbol) && NextIs(VBSymbolType.Transition));
        }

        protected override void HandleEmbeddedTransition()
        {
            HandleEmbeddedTransition(null);
        }

        protected void HandleEmbeddedTransition(VBSymbol lastWhiteSpace)
        {
            if (At(VBSymbolType.RazorCommentTransition))
            {
                Accept(lastWhiteSpace);
                RazorComment();
            }
            else if ((At(VBSymbolType.Transition) && !Was(VBSymbolType.Dot)))
            {
                HandleTransition(lastWhiteSpace);
            }
        }

        public override void ParseBlock()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(RazorResources.Parser_Context_Not_Set);
            }
            using (PushSpanConfig())
            {
                if (Context == null)
                {
                    throw new InvalidOperationException(RazorResources.Parser_Context_Not_Set);
                }

                Initialize(Span);
                NextToken();
                using (Context.StartBlock())
                {
                    IEnumerable<VBSymbol> syms = ReadWhile(sym => sym.Type == VBSymbolType.WhiteSpace);
                    if (At(VBSymbolType.Transition))
                    {
                        Accept(syms);
                        Span.CodeGenerator = new StatementCodeGenerator();
                        Output(SpanKind.Code);
                    }
                    else
                    {
                        PutBack(syms);
                        EnsureCurrent();
                    }

                    // Allow a transition span, but don't require it
                    if (Optional(VBSymbolType.Transition))
                    {
                        Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                        Span.CodeGenerator = SpanCodeGenerator.Null;
                        Output(SpanKind.Transition);
                    }

                    Context.CurrentBlock.Type = BlockType.Expression;
                    Context.CurrentBlock.CodeGenerator = new ExpressionCodeGenerator();

                    // Determine the type of the block
                    bool isComplete = false;
                    Action<SpanBuilder> config = null;
                    if (!EndOfFile)
                    {
                        switch (CurrentSymbol.Type)
                        {
                            case VBSymbolType.Identifier:
                                if (!TryDirectiveBlock(ref isComplete))
                                {
                                    ImplicitExpression();
                                }
                                break;
                            case VBSymbolType.LeftParenthesis:
                                isComplete = ExplicitExpression();
                                break;
                            case VBSymbolType.Keyword:
                                Context.CurrentBlock.Type = BlockType.Statement;
                                Context.CurrentBlock.CodeGenerator = BlockCodeGenerator.Null;
                                isComplete = KeywordBlock();
                                break;
                            case VBSymbolType.WhiteSpace:
                            case VBSymbolType.NewLine:
                                config = ImplictExpressionSpanConfig;
                                Context.OnError(CurrentLocation,
                                                RazorResources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock_VB);
                                break;
                            default:
                                config = ImplictExpressionSpanConfig;
                                Context.OnError(CurrentLocation,
                                                RazorResources.ParseError_Unexpected_Character_At_Start_Of_CodeBlock_VB,
                                                CurrentSymbol.Content);
                                break;
                        }
                    }
                    else
                    {
                        config = ImplictExpressionSpanConfig;
                        Context.OnError(CurrentLocation,
                                        RazorResources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock);
                    }
                    using (PushSpanConfig(config))
                    {
                        if (!isComplete && Span.Symbols.Count == 0 && Context.LastAcceptedCharacters != AcceptedCharacters.Any)
                        {
                            AddMarkerSymbolIfNecessary();
                        }
                        Output(SpanKind.Code);
                        PutCurrentBack();
                    }
                }
            }
        }

        private void ImplictExpressionSpanConfig(SpanBuilder span)
        {
            span.CodeGenerator = new ExpressionCodeGenerator();
            span.EditHandler = new ImplicitExpressionEditHandler(
                Language.TokenizeString,
                Keywords,
                acceptTrailingDot: DirectParentIsCode)
            {
                AcceptedCharacters = AcceptedCharacters.NonWhiteSpace
            };
        }

        private Action<SpanBuilder> StatementBlockSpanConfiguration(SpanCodeGenerator codeGenerator)
        {
            return span =>
            {
                span.Kind = SpanKind.Code;
                span.CodeGenerator = codeGenerator;
                span.EditHandler = SpanEditHandler.CreateDefault(Language.TokenizeString);
            };
        }

        // Pass "complete" flag by ref, not out because some paths may not change it.
        private bool TryDirectiveBlock(ref bool complete)
        {
            Assert(VBSymbolType.Identifier);
            Func<bool> handler;
            if (_directiveHandlers.TryGetValue(CurrentSymbol.Content, out handler))
            {
                Context.CurrentBlock.CodeGenerator = BlockCodeGenerator.Null;
                complete = handler();
                return true;
            }
            return false;
        }

        private bool KeywordBlock()
        {
            Assert(VBSymbolType.Keyword);
            Func<bool> handler;
            if (_keywordHandlers.TryGetValue(CurrentSymbol.Keyword.Value, out handler))
            {
                Span.CodeGenerator = new StatementCodeGenerator();
                Context.CurrentBlock.Type = BlockType.Statement;
                return handler();
            }
            else
            {
                ImplicitExpression();
                return false;
            }
        }

        private bool ExplicitExpression()
        {
            Context.CurrentBlock.Type = BlockType.Expression;
            Context.CurrentBlock.CodeGenerator = new ExpressionCodeGenerator();
            SourceLocation start = CurrentLocation;
            Expected(VBSymbolType.LeftParenthesis);
            Span.CodeGenerator = SpanCodeGenerator.Null;
            Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
            Output(SpanKind.MetaCode);

            Span.CodeGenerator = new ExpressionCodeGenerator();
            using (PushSpanConfig(span => span.CodeGenerator = new ExpressionCodeGenerator()))
            {
                if (!Balance(BalancingModes.NoErrorOnFailure |
                             BalancingModes.BacktrackOnFailure |
                             BalancingModes.AllowCommentsAndTemplates,
                             VBSymbolType.LeftParenthesis,
                             VBSymbolType.RightParenthesis,
                             start))
                {
                    Context.OnError(start,
                                    RazorResources.ParseError_Expected_EndOfBlock_Before_EOF,
                                    RazorResources.BlockName_ExplicitExpression,
                                    VBSymbol.GetSample(VBSymbolType.RightParenthesis),
                                    VBSymbol.GetSample(VBSymbolType.LeftParenthesis));
                    AcceptUntil(VBSymbolType.NewLine);
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Code);
                    PutCurrentBack();
                    return false;
                }
                else
                {
                    AddMarkerSymbolIfNecessary();
                    Output(SpanKind.Code);
                    Expected(VBSymbolType.RightParenthesis);
                    Span.EditHandler.AcceptedCharacters = AcceptedCharacters.None;
                    Span.CodeGenerator = SpanCodeGenerator.Null;
                    Output(SpanKind.MetaCode);
                    PutCurrentBack();
                    return true;
                }
            }
        }

        private void ImplicitExpression()
        {
            Context.CurrentBlock.Type = BlockType.Expression;
            Context.CurrentBlock.CodeGenerator = new ExpressionCodeGenerator();
            using (PushSpanConfig(ImplictExpressionSpanConfig))
            {
                Expected(VBSymbolType.Identifier, VBSymbolType.Keyword);
                Span.CodeGenerator = new ExpressionCodeGenerator();
                while (!EndOfFile)
                {
                    switch (CurrentSymbol.Type)
                    {
                        case VBSymbolType.LeftParenthesis:
                            SourceLocation start = CurrentLocation;
                            AcceptAndMoveNext();

                            Action<SpanBuilder> oldConfig = SpanConfig;
                            using (PushSpanConfig())
                            {
                                ConfigureSpan(span =>
                                {
                                    oldConfig(span);
                                    span.EditHandler.AcceptedCharacters = AcceptedCharacters.Any;
                                });
                                Balance(BalancingModes.AllowCommentsAndTemplates,
                                        VBSymbolType.LeftParenthesis,
                                        VBSymbolType.RightParenthesis,
                                        start);
                            }
                            if (Optional(VBSymbolType.RightParenthesis))
                            {
                                Span.EditHandler.AcceptedCharacters = AcceptedCharacters.NonWhiteSpace;
                            }
                            break;
                        case VBSymbolType.Dot:
                            VBSymbol dot = CurrentSymbol;
                            NextToken();
                            if (At(VBSymbolType.Identifier) || At(VBSymbolType.Keyword))
                            {
                                Accept(dot);
                                AcceptAndMoveNext();
                            }
                            else if (At(VBSymbolType.Transition))
                            {
                                VBSymbol at = CurrentSymbol;
                                NextToken();
                                if (At(VBSymbolType.Identifier) || At(VBSymbolType.Keyword))
                                {
                                    Accept(dot);
                                    Accept(at);
                                    AcceptAndMoveNext();
                                }
                                else
                                {
                                    PutBack(at);
                                    PutBack(dot);
                                }
                            }
                            else
                            {
                                PutCurrentBack();
                                if (IsNested)
                                {
                                    Accept(dot);
                                }
                                else
                                {
                                    PutBack(dot);
                                }
                                return;
                            }
                            break;
                        default:
                            PutCurrentBack();
                            return;
                    }
                }
            }
        }

        protected void MapKeyword(VBKeyword keyword, Func<bool> action)
        {
            _keywordHandlers[keyword] = action;
            Keywords.Add(keyword.ToString());
        }

        protected void MapDirective(string directive, Func<bool> action)
        {
            _directiveHandlers[directive] = action;
            Keywords.Add(directive);
        }

        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This only occurs in Release builds, where this method is empty by design")]
        [Conditional("DEBUG")]
        protected void Assert(VBKeyword keyword)
        {
            Debug.Assert(CurrentSymbol.Type == VBSymbolType.Keyword && CurrentSymbol.Keyword == keyword);
        }

        protected bool At(VBKeyword keyword)
        {
            return At(VBSymbolType.Keyword) && CurrentSymbol.Keyword == keyword;
        }

        protected void OtherParserBlock()
        {
            OtherParserBlock(null, null);
        }

        protected void OtherParserBlock(string startSequence, string endSequence)
        {
            using (PushSpanConfig())
            {
                if (Span.Symbols.Count > 0)
                {
                    Output(SpanKind.Code);
                }

                Context.SwitchActiveParser();

                bool old = DirectParentIsCode;
                DirectParentIsCode = false;

                Debug.Assert(ReferenceEquals(Context.ActiveParser, Context.MarkupParser));
                if (!String.IsNullOrEmpty(startSequence) || !String.IsNullOrEmpty(endSequence))
                {
                    Context.MarkupParser.ParseSection(Tuple.Create(startSequence, endSequence), false);
                }
                else
                {
                    Context.MarkupParser.ParseBlock();
                }

                DirectParentIsCode = old;

                Context.SwitchActiveParser();
                EnsureCurrent();
            }
            Initialize(Span);
        }

        protected void HandleTransition(VBSymbol lastWhiteSpace)
        {
            if (At(VBSymbolType.RazorCommentTransition))
            {
                Accept(lastWhiteSpace);
                RazorComment();
                return;
            }

            // Check the next character
            VBSymbol transition = CurrentSymbol;
            NextToken();
            if (At(VBSymbolType.LessThan) || At(VBSymbolType.Colon))
            {
                // Put the transition back
                PutCurrentBack();
                PutBack(transition);

                // If we're in design-time mode, accept the whitespace, otherwise put it back
                if (Context.DesignTimeMode)
                {
                    Accept(lastWhiteSpace);
                }
                else
                {
                    PutBack(lastWhiteSpace);
                }

                // Switch to markup
                OtherParserBlock();
            }
            else if (At(VBSymbolType.Transition))
            {
                if (Context.IsWithin(BlockType.Template))
                {
                    Context.OnError(transition.Start, RazorResources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested);
                }
                Accept(lastWhiteSpace);
                VBSymbol transition2 = CurrentSymbol;
                NextToken();
                if (At(VBSymbolType.LessThan) || At(VBSymbolType.Colon))
                {
                    PutCurrentBack();
                    PutBack(transition2);
                    PutBack(transition);
                    Output(SpanKind.Code);

                    // Start a template block and switch to Markup
                    using (Context.StartBlock(BlockType.Template))
                    {
                        Context.CurrentBlock.CodeGenerator = new TemplateBlockCodeGenerator();
                        OtherParserBlock();
                        Initialize(Span);
                    }
                }
                else
                {
                    Accept(transition);
                    Accept(transition2);
                }
            }
            else
            {
                Accept(lastWhiteSpace);

                PutCurrentBack();
                PutBack(transition);

                bool old = IsNested;
                IsNested = true;
                NestedBlock();
                IsNested = old;
            }
        }

        protected override void OutputSpanBeforeRazorComment()
        {
            Output(SpanKind.Code);
        }

        protected bool ReservedWord()
        {
            Context.CurrentBlock.Type = BlockType.Directive;
            Context.OnError(CurrentLocation, RazorResources.ParseError_ReservedWord, CurrentSymbol.Content);
            Span.CodeGenerator = SpanCodeGenerator.Null;
            AcceptAndMoveNext();
            Output(SpanKind.MetaCode, AcceptedCharacters.None);
            return true;
        }

        protected void NestedBlock()
        {
            using (PushSpanConfig())
            {
                Output(SpanKind.Code);

                bool old = DirectParentIsCode;
                DirectParentIsCode = true;

                ParseBlock();

                DirectParentIsCode = old;
            }
            Initialize(Span);
        }

        protected bool Required(VBSymbolType expected, string errorBase)
        {
            if (!Optional(expected))
            {
                Context.OnError(CurrentLocation, errorBase, GetCurrentSymbolDisplay());
                return false;
            }
            return true;
        }

        protected bool Optional(VBKeyword keyword)
        {
            if (At(keyword))
            {
                AcceptAndMoveNext();
                return true;
            }
            return false;
        }

        protected void AcceptVBSpaces()
        {
            Accept(ReadVBSpacesLazy());
        }

        protected IEnumerable<VBSymbol> ReadVBSpaces()
        {
            return ReadVBSpacesLazy().ToList();
        }

        public bool IsDirectiveDefined(string directive)
        {
            return _directiveHandlers.ContainsKey(directive);
        }

        private IEnumerable<VBSymbol> ReadVBSpacesLazy()
        {
            foreach (var symbol in ReadWhileLazy(sym => sym.Type == VBSymbolType.WhiteSpace))
            {
                yield return symbol;
            }
            while (At(VBSymbolType.LineContinuation))
            {
                int bookmark = CurrentLocation.AbsoluteIndex;
                VBSymbol under = CurrentSymbol;
                NextToken();
                if (At(VBSymbolType.NewLine))
                {
                    yield return under;
                    yield return CurrentSymbol;
                    NextToken();
                    foreach (var symbol in ReadVBSpaces())
                    {
                        yield return symbol;
                    }
                }
                else
                {
                    Context.Source.Position = bookmark;
                    NextToken();
                    yield break;
                }
            }
        }
    }
}
