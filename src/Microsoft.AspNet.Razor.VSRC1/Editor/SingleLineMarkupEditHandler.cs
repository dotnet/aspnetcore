// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;

namespace Microsoft.AspNet.Razor.Editor
{
    public class SingleLineMarkupEditHandler : SpanEditHandler
    {
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public SingleLineMarkupEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer)
            : base(tokenizer)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Func<T> is the recommended delegate type and requires this level of nesting.")]
        public SingleLineMarkupEditHandler(Func<string, IEnumerable<ISymbol>> tokenizer, AcceptedCharacters accepted)
            : base(tokenizer, accepted)
        {
        }
    }
}
