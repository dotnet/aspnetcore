// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Test.Framework
{
    public class BlockFactory
    {
        private SpanFactory _factory;

        public BlockFactory(SpanFactory factory)
        {
            _factory = factory;
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