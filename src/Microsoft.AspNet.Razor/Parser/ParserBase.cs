// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser
{
    public abstract class ParserBase
    {
        private ParserContext _context;

        public virtual ParserContext Context
        {
            get { return _context; }
            set
            {
                Debug.Assert(_context == null, "Context has already been set for this parser!");
                _context = value;
                _context.AssertOnOwnerTask();
            }
        }

        public virtual bool IsMarkupParser
        {
            get { return false; }
        }

        protected abstract ParserBase OtherParser { get; }

        public abstract void BuildSpan(SpanBuilder span, SourceLocation start, string content);

        public abstract void ParseBlock();

        // Markup Parsers need the ParseDocument and ParseSection methods since the markup parser is the first parser to hit the document 
        // and the logic may be different than the ParseBlock method.
        public virtual void ParseDocument()
        {
            Debug.Assert(IsMarkupParser);
            throw new NotSupportedException(RazorResources.ParserIsNotAMarkupParser);
        }

        public virtual void ParseSection(Tuple<string, string> nestingSequences, bool caseSensitive)
        {
            Debug.Assert(IsMarkupParser);
            throw new NotSupportedException(RazorResources.ParserIsNotAMarkupParser);
        }
    }
}
