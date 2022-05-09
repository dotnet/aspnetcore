// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Analyzer.Testing;

public class AnalyzerAssert
{
    public static void DiagnosticLocation(DiagnosticLocation expected, Location actual)
    {
        var actualSpan = actual.GetLineSpan();
        var actualLinePosition = actualSpan.StartLinePosition;

        // Only check line position if there is an actual line in the real diagnostic
        if (actualLinePosition.Line > 0)
        {
            if (actualLinePosition.Line + 1 != expected.Line)
            {
                throw new DiagnosticLocationAssertException(
                    expected,
                    actual,
                    $"Expected diagnostic to be on line \"{expected.Line}\" was actually on line \"{actualLinePosition.Line + 1}\"");
            }
        }

        // Only check column position if there is an actual column position in the real diagnostic
        if (actualLinePosition.Character > 0)
        {
            if (actualLinePosition.Character + 1 != expected.Column)
            {
                throw new DiagnosticLocationAssertException(
                    expected,
                    actual,
                    $"Expected diagnostic to start at column \"{expected.Column}\" was actually on column \"{actualLinePosition.Character + 1}\"");
            }
        }
    }

    private sealed class DiagnosticLocationAssertException : EqualException
    {
        public DiagnosticLocationAssertException(
            DiagnosticLocation expected,
            Location actual,
            string message)
            : base(expected, actual)
        {
            Message = message;
        }

        public override string Message { get; }
    }
}
