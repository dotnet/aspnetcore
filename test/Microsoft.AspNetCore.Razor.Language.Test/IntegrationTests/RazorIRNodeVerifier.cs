// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public static class RazorIRNodeVerifier
    {
        public static void Verify(RazorIRNode node, string[] baseline)
        {
            var walker = new Walker(baseline);
            walker.Visit(node);
        }

        private class Walker : RazorIRNodeWalker
        {
            private readonly string[] _baseline;
            private readonly RazorIRNodeWriter _visitor;
            private readonly StringWriter _writer;

            private int _index;

            public Walker(string[] baseline)
            {
                _writer = new StringWriter();

                _visitor = new RazorIRNodeWriter(_writer);
                _baseline = baseline;

            }

            public TextWriter Writer { get; }

            public override void VisitDefault(RazorIRNode node)
            {
                var expected = _index < _baseline.Length ? _baseline[_index++] : null;

                // Write the node as text for comparison
                _writer.GetStringBuilder().Clear();
                _visitor.Visit(node);
                var actual = _writer.GetStringBuilder().ToString();

                AssertNodeEquals(node, expected, actual);

                _visitor.Depth++;
                base.VisitDefault(node);
                _visitor.Depth--;
            }

            private void AssertNodeEquals(RazorIRNode node, string expected, string actual)
            {
                if (string.Equals(expected, actual))
                {
                    // YAY!!! everything is great.
                    return;
                }

                if (expected == null)
                {
                    var message = "The node is missing from baseline.";
                    throw new IRBaselineException(node, expected, actual, message);
                }

                int charsVerified = 0;
                AssertNestingEqual(node, expected, actual, ref charsVerified);
                AssertNameEqual(node, expected, actual, ref charsVerified);
                AssertDelimiter(node, expected, actual, true, ref charsVerified);
                AssertLocationEqual(node, expected, actual, ref charsVerified);
                AssertDelimiter(node, expected, actual, false, ref charsVerified);
                AssertContentEqual(node, expected, actual, ref charsVerified);

                throw new InvalidOperationException("We can't figure out HOW these two things are different. This is a bug.");
            }

            private void AssertNestingEqual(RazorIRNode node, string expected, string actual, ref int charsVerified)
            {
                var i = 0;
                for (; i < expected.Length; i++)
                {
                    if (expected[i] != ' ')
                    {
                        break;
                    }
                }

                var failed = false;
                var j = 0;
                for (; j < i; j++)
                {
                    if (actual.Length <= j || actual[j] != ' ')
                    {
                        failed = true;
                        break;
                    }
                }

                if (actual.Length <= j + 1 || actual[j] == ' ')
                {
                    failed = true;
                }

                if (failed)
                {
                    var message = "The node is at the wrong level of nesting. This usually means a child is missing.";
                    throw new IRBaselineException(node, expected, actual, message);
                }

                charsVerified = j;
            }

            private void AssertNameEqual(RazorIRNode node, string expected, string actual, ref int charsVerified)
            {
                var expectedName = GetName(expected, charsVerified);
                var actualName = GetName(actual, charsVerified);

                if (!string.Equals(expectedName, actualName))
                {
                    var message = $"Node names are not equal.";
                    throw new IRBaselineException(node, expected, actual, message);
                }

                charsVerified += expectedName.Length;
            }

            // Either both strings need to have a delimiter next or neither should.
            private void AssertDelimiter(RazorIRNode node, string expected, string actual, bool required, ref int charsVerified)
            {
                if (charsVerified == expected.Length && required)
                {
                    throw new InvalidOperationException($"Baseline text is not well-formed: '{expected}'.");
                }

                if (charsVerified == actual.Length && required)
                {
                    throw new InvalidOperationException($"Baseline text is not well-formed: '{actual}'.");
                }

                if (charsVerified == expected.Length && charsVerified == actual.Length)
                {
                    return;
                }

                var expectedDelimiter = expected.IndexOf(" - ", charsVerified);
                if (expectedDelimiter != charsVerified && expectedDelimiter != -1)
                {
                    throw new InvalidOperationException($"Baseline text is not well-formed: '{actual}'.");
                }

                var actualDelimiter = actual.IndexOf(" - ", charsVerified);
                if (actualDelimiter != charsVerified && actualDelimiter != -1)
                {
                    throw new InvalidOperationException($"Baseline text is not well-formed: '{actual}'.");
                }

                Assert.Equal(expectedDelimiter, actualDelimiter);

                charsVerified += 3;
            }

            private void AssertLocationEqual(RazorIRNode node, string expected, string actual, ref int charsVerified)
            {
                var expectedLocation = GetLocation(expected, charsVerified);
                var actualLocation = GetLocation(actual, charsVerified);

                if (!string.Equals(expectedLocation, actualLocation))
                {
                    var message = $"Locations are not equal.";
                    throw new IRBaselineException(node, expected, actual, message);
                }

                charsVerified += expectedLocation.Length;
            }

            private void AssertContentEqual(RazorIRNode node, string expected, string actual, ref int charsVerified)
            {
                var expectedContent = GetContent(expected, charsVerified);
                var actualContent = GetContent(actual, charsVerified);

                if (!string.Equals(expectedContent, actualContent))
                {
                    var message = $"Contents are not equal.";
                    throw new IRBaselineException(node, expected, actual, message);
                }

                charsVerified += expectedContent.Length;
            }

            private string GetName(string text, int start)
            {
                var delimiter = text.IndexOf(" - ", start);
                if (delimiter == -1)
                {
                    throw new InvalidOperationException($"Baseline text is not well-formed: '{text}'.");
                }

                return text.Substring(start, delimiter - start);
            }

            private string GetLocation(string text, int start)
            {
                var delimiter = text.IndexOf(" - ", start);
                return delimiter == -1 ? text.Substring(start) : text.Substring(start, delimiter - start);
            }

            private string GetContent(string text, int start)
            {
                return start == text.Length ? string.Empty : text.Substring(start);
            }

            private class IRBaselineException : XunitException
            {
                public IRBaselineException(RazorIRNode node, string expected, string actual, string userMessage)
                    : base(Format(node, expected, actual, userMessage))
                {
                    Node = node;
                    Expected = expected;
                    Actual = actual;
                }

                public RazorIRNode Node { get; }

                public string Actual { get; }

                public string Expected { get; }

                private static string Format(RazorIRNode node, string expected, string actual, string userMessage)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine(userMessage);
                    builder.AppendLine();

                    if (expected != null)
                    {
                        builder.Append("Expected: ");
                        builder.AppendLine(expected);
                    }

                    if (actual != null)
                    {
                        builder.Append("Actual: ");
                        builder.AppendLine(actual);
                    }

                    builder.AppendLine();
                    builder.AppendLine("Path:");

                    var current = node;
                    do
                    {
                        builder.AppendLine(current.ToString());
                    }
                    while ((current = current.Parent) != null);

                    return builder.ToString();
                }
            }
        }
    }
}
