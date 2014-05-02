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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Text;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class AutoCompleteEditHandler : SpanEditHandler
    {
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public AutoCompleteEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer)
            : base(tokenizer)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public AutoCompleteEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, AcceptedCharacters accepted)
            : base(tokenizer, accepted)
        {
        }

        public bool AutoCompleteAtEndOfSpan { get; set; }
        public string AutoCompleteString { get; set; }

        protected override PartialParseResult CanAcceptChange(Span target, TextChange normalizedChange)
        {
            if (((AutoCompleteAtEndOfSpan && IsAtEndOfSpan(target, normalizedChange)) || IsAtEndOfFirstLine(target, normalizedChange)) &&
                normalizedChange.IsInsert &&
                ParserHelpers.IsNewLine(normalizedChange.NewText) &&
                AutoCompleteString != null)
            {
                return PartialParseResult.Rejected | PartialParseResult.AutoCompleteBlock;
            }
            return PartialParseResult.Rejected;
        }

        public override string ToString()
        {
            return base.ToString() + ",AutoComplete:[" + (AutoCompleteString ?? "<null>") + "]" + (AutoCompleteAtEndOfSpan ? ";AtEnd" : ";AtEOL");
        }

        public override bool Equals(object obj)
        {
            AutoCompleteEditHandler other = obj as AutoCompleteEditHandler;
            return base.Equals(obj) &&
                   other != null &&
                   String.Equals(other.AutoCompleteString, AutoCompleteString, StringComparison.Ordinal) &&
                   AutoCompleteAtEndOfSpan == other.AutoCompleteAtEndOfSpan;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(AutoCompleteString)
                .CombinedHash;
        }
    }
}
