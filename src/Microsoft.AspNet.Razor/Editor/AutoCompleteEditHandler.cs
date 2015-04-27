// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        private static readonly int TypeHashCode = typeof(AutoCompleteEditHandler).GetHashCode();

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public AutoCompleteEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer)
            : base(tokenizer)
        {
        }

        public AutoCompleteEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, bool autoCompleteAtEndOfSpan)
            : this(tokenizer)
        {
            AutoCompleteAtEndOfSpan = autoCompleteAtEndOfSpan;
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public AutoCompleteEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, AcceptedCharacters accepted)
            : base(tokenizer, accepted)
        {
        }

        public bool AutoCompleteAtEndOfSpan { get; }

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
            var other = obj as AutoCompleteEditHandler;
            return base.Equals(other) &&
                string.Equals(other.AutoCompleteString, AutoCompleteString, StringComparison.Ordinal) &&
                AutoCompleteAtEndOfSpan == other.AutoCompleteAtEndOfSpan;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties but Equals also checks the type.
            return HashCodeCombiner.Start()
                .Add(TypeHashCode)
                .Add(AutoCompleteAtEndOfSpan)
                .CombinedHash;
        }
    }
}
