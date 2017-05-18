// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
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
            return EscapedMarkupTagBlock(prefix, suffix, AcceptedCharactersInternal.Any);
        }

        public Block EscapedMarkupTagBlock(string prefix, string suffix, params SyntaxTreeNode[] children)
        {
            return EscapedMarkupTagBlock(prefix, suffix, AcceptedCharactersInternal.Any, children);
        }

        public Block EscapedMarkupTagBlock(
            string prefix,
            string suffix,
            AcceptedCharactersInternal acceptedCharacters,
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
            return MarkupTagBlock(content, AcceptedCharactersInternal.Any);
        }

        public Block MarkupTagBlock(string content, AcceptedCharactersInternal acceptedCharacters)
        {
            return new MarkupTagBlock(
                _factory.Markup(content).Accepts(acceptedCharacters)
            );
        }

        public Block TagHelperBlock(
            string tagName,
            TagMode tagMode,
            SourceLocation start,
            Block startTag,
            SyntaxTreeNode[] children,
            Block endTag)
        {
            var builder = new TagHelperBlockBuilder(
                tagName,
                tagMode,
                attributes: new List<TagHelperAttributeNode>(),
                children: children)
            {
                Start = start,
                SourceStartTag = startTag,
                SourceEndTag = endTag
            };

            return builder.Build();
        }
    }
}