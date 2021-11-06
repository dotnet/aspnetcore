// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class AutoCompleteEditHandler : SpanEditHandler
{
    private static readonly int TypeHashCode = typeof(AutoCompleteEditHandler).GetHashCode();

    public AutoCompleteEditHandler(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer)
        : base(tokenizer)
    {
    }

    public AutoCompleteEditHandler(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer, bool autoCompleteAtEndOfSpan)
        : this(tokenizer)
    {
        AutoCompleteAtEndOfSpan = autoCompleteAtEndOfSpan;
    }

    public AutoCompleteEditHandler(Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer, AcceptedCharactersInternal accepted)
        : base(tokenizer, accepted)
    {
    }

    public bool AutoCompleteAtEndOfSpan { get; }

    public string AutoCompleteString { get; set; }

    protected override PartialParseResultInternal CanAcceptChange(SyntaxNode target, SourceChange change)
    {
        if (((AutoCompleteAtEndOfSpan && IsAtEndOfSpan(target, change)) || IsAtEndOfFirstLine(target, change)) &&
            change.IsInsert &&
            ParserHelpers.IsNewLine(change.NewText) &&
            AutoCompleteString != null)
        {
            return PartialParseResultInternal.Rejected | PartialParseResultInternal.AutoCompleteBlock;
        }
        return PartialParseResultInternal.Rejected;
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
        var hashCodeCombiner = HashCodeCombiner.Start();
        hashCodeCombiner.Add(TypeHashCode);
        hashCodeCombiner.Add(AutoCompleteAtEndOfSpan);

        return hashCodeCombiner.CombinedHash;
    }
}
