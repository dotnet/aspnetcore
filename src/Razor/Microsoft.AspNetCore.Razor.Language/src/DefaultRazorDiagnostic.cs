// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorDiagnostic : RazorDiagnostic
{
    internal DefaultRazorDiagnostic(RazorDiagnosticDescriptor descriptor, SourceSpan span, object[] args)
    {
        Descriptor = descriptor;
        Span = span;
        Args = args;
    }

    public override string Id => Descriptor.Id;

    public override RazorDiagnosticSeverity Severity => Descriptor.Severity;

    public override SourceSpan Span { get; }

    // Internal for testing
    internal RazorDiagnosticDescriptor Descriptor { get; }

    // Internal for testing
    internal object[] Args { get; }

    public override string GetMessage(IFormatProvider formatProvider)
    {
        var format = Descriptor.GetMessageFormat();
        return string.Format(formatProvider, format, Args);
    }

    public override bool Equals(RazorDiagnostic obj)
    {
        var other = obj as DefaultRazorDiagnostic;
        if (other == null)
        {
            return false;
        }

        if (!Descriptor.Equals(other.Descriptor))
        {
            return false;
        }

        if (!Span.Equals(other.Span))
        {
            return false;
        }

        if (Args.Length != other.Args.Length)
        {
            return false;
        }

        for (var i = 0; i < Args.Length; i++)
        {
            if (!Args[i].Equals(other.Args[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(Descriptor.GetHashCode());
        hash.Add(Span.GetHashCode());

        for (var i = 0; i < Args.Length; i++)
        {
            hash.Add(Args[i]);
        }

        return hash;
    }
}
