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
    public class CSharpSymbol : SymbolBase<CSharpSymbolType>
    {
        // Helper constructor
        public CSharpSymbol(int offset, int line, int column, string content, CSharpSymbolType type)
            : this(new SourceLocation(offset, line, column), content, type, Enumerable.Empty<RazorError>())
        {
        }

        public CSharpSymbol(SourceLocation start, string content, CSharpSymbolType type)
            : this(start, content, type, Enumerable.Empty<RazorError>())
        {
        }

        public CSharpSymbol(int offset, int line, int column, string content, CSharpSymbolType type, IEnumerable<RazorError> errors)
            : base(new SourceLocation(offset, line, column), content, type, errors)
        {
        }

        public CSharpSymbol(SourceLocation start, string content, CSharpSymbolType type, IEnumerable<RazorError> errors)
            : base(start, content, type, errors)
        {
        }

        public bool? EscapedIdentifier { get; set; }
        public CSharpKeyword? Keyword { get; set; }

        public override bool Equals(object obj)
        {
            CSharpSymbol other = obj as CSharpSymbol;
            return base.Equals(obj) && other.Keyword == Keyword;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Keyword.GetHashCode();
        }
    }
}
