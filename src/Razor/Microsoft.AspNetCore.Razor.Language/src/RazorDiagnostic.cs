// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorDiagnostic : IEquatable<RazorDiagnostic>, IFormattable
{
    internal static readonly RazorDiagnostic[] EmptyArray = Array.Empty<RazorDiagnostic>();
    internal static readonly object[] EmptyArgs = Array.Empty<object>();

    public abstract string Id { get; }

    public abstract RazorDiagnosticSeverity Severity { get; }

    public abstract SourceSpan Span { get; }

    public abstract string GetMessage(IFormatProvider formatProvider);

    public string GetMessage() => GetMessage(null);

    public abstract bool Equals(RazorDiagnostic other);

    public abstract override int GetHashCode();

    public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan span)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return new DefaultRazorDiagnostic(descriptor, span, EmptyArgs);
    }

    public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan span, params object[] args)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return new DefaultRazorDiagnostic(descriptor, span, args);
    }

    public override string ToString()
    {
        return ((IFormattable)this).ToString(null, null);
    }

    public override bool Equals(object obj)
    {
        var other = obj as RazorDiagnostic;
        return other == null ? false : Equals(other);
    }

    string IFormattable.ToString(string ignore, IFormatProvider formatProvider)
    {
        // Our indices are 0-based, but we we want to print them as 1-based.
        return $"{Span.FilePath}({Span.LineIndex + 1},{Span.CharacterIndex + 1}): {Severity} {Id}: {GetMessage(formatProvider)}";
    }
}
