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
