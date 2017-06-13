// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SyntaxTreeBuilder
    {
        private readonly Stack<BlockBuilder> _blockStack;
        private readonly Action _endBlock;

        public SyntaxTreeBuilder()
        {
            _blockStack = new Stack<BlockBuilder>();
            _endBlock = EndBlock;
        }

        public IReadOnlyCollection<BlockBuilder> ActiveBlocks => _blockStack;

        public BlockBuilder CurrentBlock => _blockStack.Peek();

        public Span LastSpan { get; private set; }

        public AcceptedCharactersInternal LastAcceptedCharacters
        {
            get
            {
                if (LastSpan == null)
                {
                    return AcceptedCharactersInternal.None;
                }
                return LastSpan.EditHandler.AcceptedCharacters;
            }
        }

        public void Add(Span span)
        {
            if (_blockStack.Count == 0)
            {
                throw new InvalidOperationException(LegacyResources.ParserContext_NoCurrentBlock);
            }
            CurrentBlock.Children.Add(span);
            LastSpan = span;
        }

        /// <summary>
        /// Starts a block of the specified type
        /// </summary>
        /// <param name="blockType">The type of the block to start</param>
        public IDisposable StartBlock(BlockKindInternal blockType)
        {
            var builder = new BlockBuilder() { Type = blockType };
            _blockStack.Push(builder);
            return new DisposableAction(_endBlock);
        }

        /// <summary>
        /// Ends the current block
        /// </summary>
        public void EndBlock()
        {
            if (_blockStack.Count == 0)
            {
                throw new InvalidOperationException(LegacyResources.EndBlock_Called_Without_Matching_StartBlock);
            }

            if (_blockStack.Count > 1)
            {
                var initialBlockBuilder = _blockStack.Pop();
                var initialBlock = initialBlockBuilder.Build();
                CurrentBlock.Children.Add(initialBlock);
            }
        }

        public Block Build()
        {
            if (_blockStack.Count == 0)
            {
                throw new InvalidOperationException(LegacyResources.ParserContext_CannotCompleteTree_NoRootBlock);
            }
            if (_blockStack.Count != 1)
            {
                throw new InvalidOperationException(LegacyResources.ParserContext_CannotCompleteTree_OutstandingBlocks);
            }

            var rootBuilder = _blockStack.Pop();
            var root = rootBuilder.Build();

            return root;
        }
    }
}
