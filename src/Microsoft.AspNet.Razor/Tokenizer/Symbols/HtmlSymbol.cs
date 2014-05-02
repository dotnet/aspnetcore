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

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Tokenizer.Symbols
{
    public class HtmlSymbol : SymbolBase<HtmlSymbolType>
    {
        // Helper constructor
        public HtmlSymbol(int offset, int line, int column, string content, HtmlSymbolType type)
            : this(new SourceLocation(offset, line, column), content, type, Enumerable.Empty<RazorError>())
        {
        }

        public HtmlSymbol(SourceLocation start, string content, HtmlSymbolType type)
            : base(start, content, type, Enumerable.Empty<RazorError>())
        {
        }

        public HtmlSymbol(int offset, int line, int column, string content, HtmlSymbolType type, IEnumerable<RazorError> errors)
            : base(new SourceLocation(offset, line, column), content, type, errors)
        {
        }

        public HtmlSymbol(SourceLocation start, string content, HtmlSymbolType type, IEnumerable<RazorError> errors)
            : base(start, content, type, errors)
        {
        }
    }
}
