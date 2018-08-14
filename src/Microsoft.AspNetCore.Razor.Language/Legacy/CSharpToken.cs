// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class CSharpToken : TokenBase<CSharpTokenType>
    {
        public CSharpToken(
            string content,
            CSharpTokenType type)
            : base(content, type, RazorDiagnostic.EmptyArray)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public CSharpToken(
            string content,
            CSharpTokenType type,
            IReadOnlyList<RazorDiagnostic> errors)
            : base(content, type, errors)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
        }

        public CSharpKeyword? Keyword { get; set; }

        protected override SyntaxToken GetSyntaxToken()
        {
            switch (Type)
            {
                default:
                    return SyntaxFactory.UnknownToken(Content, Errors.ToArray());
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as CSharpToken;
            return base.Equals(other) &&
                other.Keyword == Keyword;
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            return base.GetHashCode();
        }
    }
}
