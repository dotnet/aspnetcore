// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Utils;

namespace Microsoft.AspNet.Razor.Parser
{
    public partial class ParserContext
    {
        private int? _ownerTaskId;

        private bool _terminated = false;

        private Stack<BlockBuilder> _blockStack = new Stack<BlockBuilder>();

        public ParserContext(ITextDocument source, ParserBase codeParser, ParserBase markupParser, ParserBase activeParser)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (codeParser == null)
            {
                throw new ArgumentNullException("codeParser");
            }
            if (markupParser == null)
            {
                throw new ArgumentNullException("markupParser");
            }
            if (activeParser == null)
            {
                throw new ArgumentNullException("activeParser");
            }
            if (activeParser != codeParser && activeParser != markupParser)
            {
                throw new ArgumentException(RazorResources.ActiveParser_Must_Be_Code_Or_Markup_Parser, "activeParser");
            }

            CaptureOwnerTask();

            Source = new TextDocumentReader(source);
            CodeParser = codeParser;
            MarkupParser = markupParser;
            ActiveParser = activeParser;
            Errors = new List<RazorError>();
        }

        public IList<RazorError> Errors { get; private set; }
        public TextDocumentReader Source { get; set; }
        public ParserBase CodeParser { get; private set; }
        public ParserBase MarkupParser { get; private set; }
        public ParserBase ActiveParser { get; private set; }
        public bool DesignTimeMode { get; set; }

        public BlockBuilder CurrentBlock
        {
            get { return _blockStack.Peek(); }
        }

        public Span LastSpan { get; private set; }
        public bool WhiteSpaceIsSignificantToAncestorBlock { get; set; }

        public AcceptedCharacters LastAcceptedCharacters
        {
            get
            {
                if (LastSpan == null)
                {
                    return AcceptedCharacters.None;
                }
                return LastSpan.EditHandler.AcceptedCharacters;
            }
        }

        internal Stack<BlockBuilder> BlockStack
        {
            get { return _blockStack; }
        }

        public char CurrentCharacter
        {
            get
            {
                if (_terminated)
                {
                    return '\0';
                }
#if DEBUG
                if (CheckInfiniteLoop())
                {
                    return '\0';
                }
#endif
                var ch = Source.Peek();
                if (ch == -1)
                {
                    return '\0';
                }
                return (char)ch;
            }
        }

        public bool EndOfFile
        {
            get { return _terminated || Source.Peek() == -1; }
        }

        public void AddSpan(Span span)
        {
            EnusreNotTerminated();
            if (_blockStack.Count == 0)
            {
                throw new InvalidOperationException(RazorResources.ParserContext_NoCurrentBlock);
            }
            _blockStack.Peek().Children.Add(span);
            LastSpan = span;
        }

        /// <summary>
        /// Starts a block of the specified type
        /// </summary>
        /// <param name="blockType">The type of the block to start</param>
        public IDisposable StartBlock(BlockType blockType)
        {
            EnusreNotTerminated();
            AssertOnOwnerTask();
            _blockStack.Push(new BlockBuilder() { Type = blockType });
            return new DisposableAction(EndBlock);
        }

        /// <summary>
        /// Starts a block
        /// </summary>
        public IDisposable StartBlock()
        {
            EnusreNotTerminated();
            AssertOnOwnerTask();
            _blockStack.Push(new BlockBuilder());
            return new DisposableAction(EndBlock);
        }

        /// <summary>
        /// Ends the current block
        /// </summary>
        public void EndBlock()
        {
            EnusreNotTerminated();
            AssertOnOwnerTask();

            if (_blockStack.Count == 0)
            {
                throw new InvalidOperationException(RazorResources.EndBlock_Called_Without_Matching_StartBlock);
            }
            if (_blockStack.Count > 1)
            {
                var block = _blockStack.Pop();
                _blockStack.Peek().Children.Add(block.Build());
            }
            else
            {
                // If we're at 1, terminate the parser
                _terminated = true;
            }
        }

        /// <summary>
        /// Gets a boolean indicating if any of the ancestors of the current block is of the specified type
        /// </summary>
        public bool IsWithin(BlockType type)
        {
            return _blockStack.Any(b => b.Type == type);
        }

        public void SwitchActiveParser()
        {
            EnusreNotTerminated();
            AssertOnOwnerTask();
            if (ReferenceEquals(ActiveParser, CodeParser))
            {
                ActiveParser = MarkupParser;
            }
            else
            {
                ActiveParser = CodeParser;
            }
        }

        public void OnError(SourceLocation location, string message)
        {
            EnusreNotTerminated();
            AssertOnOwnerTask();
            Errors.Add(new RazorError(message, location));
        }

        public void OnError(SourceLocation location, string message, params object[] args)
        {
            EnusreNotTerminated();
            AssertOnOwnerTask();
            OnError(location, String.Format(CultureInfo.CurrentCulture, message, args));
        }

        public ParserResults CompleteParse()
        {
            if (_blockStack.Count == 0)
            {
                throw new InvalidOperationException(RazorResources.ParserContext_CannotCompleteTree_NoRootBlock);
            }
            if (_blockStack.Count != 1)
            {
                throw new InvalidOperationException(RazorResources.ParserContext_CannotCompleteTree_OutstandingBlocks);
            }
            return new ParserResults(_blockStack.Pop().Build(), Errors);
        }

        [Conditional("DEBUG")]
        internal void CaptureOwnerTask()
        {
            if (Task.CurrentId != null)
            {
                _ownerTaskId = Task.CurrentId;
            }
        }

        [Conditional("DEBUG")]
        internal void AssertOnOwnerTask()
        {
            if (_ownerTaskId != null)
            {
                Debug.Assert(_ownerTaskId == Task.CurrentId);
            }
        }

        [Conditional("DEBUG")]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "The method body is empty in Release builds")]
        internal void AssertCurrent(char expected)
        {
            Debug.Assert(CurrentCharacter == expected);
        }

        private void EnusreNotTerminated()
        {
            if (_terminated)
            {
                throw new InvalidOperationException(RazorResources.ParserContext_ParseComplete);
            }
        }
    }

    // Debug Helpers

#if DEBUG
    [DebuggerDisplay("{Unparsed}")]
    public partial class ParserContext
    {
        private const int InfiniteLoopCountThreshold = 1000;
        private int _infiniteLoopGuardCount = 0;
        private SourceLocation? _infiniteLoopGuardLocation = null;

        internal string Unparsed
        {
            get
            {
                var remaining = Source.ReadToEnd();
                Source.Position -= remaining.Length;
                return remaining;
            }
        }

        private bool CheckInfiniteLoop()
        {
            // Infinite loop guard
            //  Basically, if this property is accessed 1000 times in a row without having advanced the source reader to the next position, we
            //  cause a parser error
            if (_infiniteLoopGuardLocation != null)
            {
                if (Source.Location == _infiniteLoopGuardLocation.Value)
                {
                    _infiniteLoopGuardCount++;
                    if (_infiniteLoopGuardCount > InfiniteLoopCountThreshold)
                    {
#if NET45
                        // No Debug.Fail in CoreCLR

                        Debug.Fail("An internal parser error is causing an infinite loop at this location.");
#else
                        Debug.Assert(false, "An internal parser error is causing an infinite loop at this location.");
#endif
                        _terminated = true;
                        return true;
                    }
                }
                else
                {
                    _infiniteLoopGuardCount = 0;
                }
            }
            _infiniteLoopGuardLocation = Source.Location;
            return false;
        }
    }
#endif
}
