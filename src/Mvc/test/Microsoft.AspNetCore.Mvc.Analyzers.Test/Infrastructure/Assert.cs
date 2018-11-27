// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal class Assert : Xunit.Assert
    {
        public static void DiagnosticsEqual(IEnumerable<DiagnosticResult> expected, IEnumerable<Diagnostic> actual)
        {
            var expectedCount = expected.Count();
            var actualCount = actual.Count();

            if (expectedCount != actualCount)
            {
                throw new DiagnosticsAssertException(
                    expected,
                    actual,
                    $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}.");
            }

            foreach (var (expectedItem, actualItem) in expected.Zip(actual, (a, b) => (a, b)))
            {
                if (expectedItem.Line == -1 && expectedItem.Column == -1)
                {
                    if (actualItem.Location != Location.None)
                    {
                        throw new DiagnosticAssertException(
                            expectedItem,
                            actualItem,
                            $"Expected: A project diagnostic with no location. Actual {actualItem.Location}.");
                    }
                }
                else
                {
                    VerifyLocation(expectedItem, actualItem);
                }

                if (actualItem.Id != expectedItem.Id)
                {
                    throw new DiagnosticAssertException(
                        expectedItem,
                        actualItem,
                        $"Expected: Expected id: {expectedItem.Id}. Actual id: {actualItem.Id}.");
                }

                if (actualItem.Severity != expectedItem.Severity)
                {
                    throw new DiagnosticAssertException(
                        expectedItem,
                        actualItem,
                        $"Expected: Expected severity: {expectedItem.Severity}. Actual severity: {actualItem.Severity}.");
                }

                if (actualItem.GetMessage() != expectedItem.Message)
                {
                    throw new DiagnosticAssertException(
                        expectedItem,
                        actualItem,
                        $"Expected: Expected message: {expectedItem.Message}. Actual message: {actualItem.GetMessage()}.");
                }
            }
        }

        private static void VerifyLocation(DiagnosticResult expected, Diagnostic actual)
        {
            if (expected.Locations.Length == 0)
            {
                return;
            }

            var expectedLocation = expected.Locations[0];
            Assert.DiagnosticLocation(expectedLocation, actual.Location);

        }

        public static void DiagnosticLocation(DiagnosticResultLocation expected, Location actual)
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
                        $"Expected diagnostic to start at column \"{expected.Column}\" was actually on line \"{actualLinePosition.Character + 1}\"");
                }
            }
        }

        private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return string.Join(Environment.NewLine, diagnostics.Select(FormatDiagnostic));
        }

        private static string FormatDiagnostic(Diagnostic diagnostic)
        {
            var builder = new StringBuilder();
            builder.AppendLine(diagnostic.ToString());

            var location = diagnostic.Location;
            if (location == Location.None)
            {
                builder.Append($"Location unknown: ({diagnostic.Id})");
            }
            else
            {
                True(location.IsInSource,
                    $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostic}");

                var linePosition = location.GetLineSpan().StartLinePosition;
                builder.Append($"({(linePosition.Line + 1)}, {(linePosition.Character + 1)}, {diagnostic.Id})");
            }

            return builder.ToString();
        }

        private static async Task<string> GetStringFromDocumentAsync(Document document)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation);
            var root = await simplifiedDoc.GetSyntaxRootAsync();
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }

        private class DiagnosticsAssertException : Xunit.Sdk.EqualException
        {
            public DiagnosticsAssertException(
                IEnumerable<DiagnosticResult> expected,
                IEnumerable<Diagnostic> actual,
                string message)
                : base(expected, actual)
            {
                Message = message + Environment.NewLine + FormatDiagnostics(actual);
            }

            public override string Message { get; }
        }

        private class DiagnosticAssertException : Xunit.Sdk.EqualException
        {
            public DiagnosticAssertException(
                DiagnosticResult expected,
                Diagnostic actual,
                string message)
                : base(expected, actual)
            {
                Message = message + Environment.NewLine + FormatDiagnostic(actual);
            }

            public override string Message { get; }
        }

        private class DiagnosticLocationAssertException : Xunit.Sdk.EqualException
        {
            public DiagnosticLocationAssertException(
                DiagnosticResultLocation expected,
                Location actual,
                string message)
                : base(expected, actual)
            {
                Message = message;
            }

            public override string Message { get; }
        }

    }
}
