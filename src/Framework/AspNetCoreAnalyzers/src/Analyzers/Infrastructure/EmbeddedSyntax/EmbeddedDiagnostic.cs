// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;

internal struct EmbeddedDiagnostic : IEquatable<EmbeddedDiagnostic>
{
    public readonly string Message;
    public readonly TextSpan Span;

    public EmbeddedDiagnostic(string message, TextSpan span)
    {
        AnalyzerDebug.Assert(message != null);
        Message = message;
        Span = span;
    }

    public override bool Equals(object? obj)
        => obj is EmbeddedDiagnostic diagnostic && Equals(diagnostic);

    public bool Equals(EmbeddedDiagnostic other)
        => Message == other.Message &&
           Span.Equals(other.Span);

    public override string ToString()
        => Message;

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = -954867195;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
            hashCode = hashCode * -1521134295 + EqualityComparer<TextSpan>.Default.GetHashCode(Span);
            return hashCode;
        }
    }

    public static bool operator ==(EmbeddedDiagnostic diagnostic1, EmbeddedDiagnostic diagnostic2)
        => diagnostic1.Equals(diagnostic2);

    public static bool operator !=(EmbeddedDiagnostic diagnostic1, EmbeddedDiagnostic diagnostic2)
        => !(diagnostic1 == diagnostic2);
}
