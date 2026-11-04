// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;

internal struct EmbeddedSyntaxToken<TSyntaxKind> where TSyntaxKind : struct
{
    public readonly TSyntaxKind Kind;
    public readonly VirtualCharSequence VirtualChars;
    internal readonly ImmutableArray<EmbeddedDiagnostic> Diagnostics;

    /// <summary>
    /// Returns the value of the token. For example, if the token represents an integer capture,
    /// then this property would return the actual integer.
    /// </summary>
    public readonly object? Value;

    public EmbeddedSyntaxToken(
        TSyntaxKind kind,
        VirtualCharSequence virtualChars,
        ImmutableArray<EmbeddedDiagnostic> diagnostics, object? value)
    {
        Debug.Assert(!diagnostics.IsDefault);
        Kind = kind;
        VirtualChars = virtualChars;
        Diagnostics = diagnostics;
        Value = value;
    }

    public bool IsMissing => VirtualChars.Length == 0;

    public EmbeddedSyntaxToken<TSyntaxKind> AddDiagnosticIfNone(EmbeddedDiagnostic diagnostic)
        => Diagnostics.Length > 0 ? this : WithDiagnostics(ImmutableArray.Create(diagnostic));

    public EmbeddedSyntaxToken<TSyntaxKind> WithDiagnostics(ImmutableArray<EmbeddedDiagnostic> diagnostics)
        => With(diagnostics: diagnostics);

    public EmbeddedSyntaxToken<TSyntaxKind> With(
        Optional<TSyntaxKind> kind = default,
        Optional<VirtualCharSequence> virtualChars = default,
        Optional<ImmutableArray<EmbeddedDiagnostic>> diagnostics = default,
        Optional<object> value = default)
    {
        return new EmbeddedSyntaxToken<TSyntaxKind>(
            kind.HasValue ? kind.Value : Kind,
            virtualChars.HasValue ? virtualChars.Value : VirtualChars,
            diagnostics.HasValue ? diagnostics.Value : Diagnostics,
            value.HasValue ? value.Value : Value);
    }

    public TextSpan GetSpan()
        => EmbeddedSyntaxHelpers.GetSpan(VirtualChars);

    public TextSpan? GetFullSpan()
    {
        if (VirtualChars.Length == 0)
        {
            return null;
        }

        var start = VirtualChars.Length == 0 ? int.MaxValue : VirtualChars[0].Span.Start;
        var end = VirtualChars.Length == 0 ? int.MinValue : VirtualChars[VirtualChars.Length - 1].Span.End;

        return TextSpan.FromBounds(start, end);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        WriteTo(sb);
        return sb.ToString();
    }

    /// <summary>
    /// Writes the token to a stringbuilder.
    /// </summary>
    public void WriteTo(StringBuilder sb)
    {
        sb.Append(VirtualChars.CreateString());
    }
}
