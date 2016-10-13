// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal abstract class ParserBase
    {
        private ParserContext _context;

        public ParserBase(ParserContext context)
        {
            Context = context;
        }

        public ParserContext Context { get; }

        public abstract void BuildSpan(SpanBuilder span, SourceLocation start, string content);

        public abstract void ParseBlock();
    }
}
