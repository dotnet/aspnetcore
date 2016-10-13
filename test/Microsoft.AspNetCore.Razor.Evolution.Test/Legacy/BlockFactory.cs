// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class BlockFactory
    {
        private SpanFactory _factory;

        public BlockFactory(SpanFactory factory)
        {
            _factory = factory;
        }

        public Block EscapedMarkupTagBlock(string prefix, string suffix)
        {
            return EscapedMarkupTagBlock(prefix, suffix, AcceptedCharacters.Any);
        }

        public Block EscapedMarkupTagBlock(string prefix, string suffix, params SyntaxTreeNode[] children)
        {
            return EscapedMarkupTagBlock(prefix, suffix, AcceptedCharacters.Any, children);
        }

        public Block EscapedMarkupTagBlock(
            string prefix,
            string suffix,
            AcceptedCharacters acceptedCharacters,
            params SyntaxTreeNode[] children)
        {
            var newChildren = new List<SyntaxTreeNode>(
                new SyntaxTreeNode[]
                {
                    _factory.Markup(prefix),
                    _factory.BangEscape(),
                    _factory.Markup(suffix).Accepts(acceptedCharacters)
                });

            newChildren.AddRange(children);

            return new MarkupTagBlock(newChildren.ToArray());
        }

        public Block MarkupTagBlock(string content)
        {
            return MarkupTagBlock(content, AcceptedCharacters.Any);
        }

        public Block MarkupTagBlock(string content, AcceptedCharacters acceptedCharacters)
        {
            return new MarkupTagBlock(
                _factory.Markup(content).Accepts(acceptedCharacters)
            );
        }
    }
}