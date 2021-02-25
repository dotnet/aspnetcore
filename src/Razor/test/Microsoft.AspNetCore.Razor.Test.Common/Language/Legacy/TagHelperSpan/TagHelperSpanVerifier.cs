// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class TagHelperSpanVerifier
    {
        internal static void Verify(RazorSyntaxTree syntaxTree, string[] baseline)
        {
            using (var writer = new StringWriter())
            {
                var walker = new Walker(writer, syntaxTree, baseline);
                walker.Visit();
                walker.AssertReachedEndOfBaseline();
            }
        }

        private class Walker : TagHelperSpanWriter
        {
            private readonly string[] _baseline;
            private readonly StringWriter _writer;

            private int _index;

            public Walker(StringWriter writer, RazorSyntaxTree syntaxTree, string[] baseline) : base(writer, syntaxTree)
            {
                _writer = writer;
                _baseline = baseline;
            }

            public override void VisitTagHelperSpan(TagHelperSpanInternal span)
            {
                var expected = _index < _baseline.Length ? _baseline[_index++] : null;

                _writer.GetStringBuilder().Clear();
                base.VisitTagHelperSpan(span);
                var actual = _writer.GetStringBuilder().ToString();
                AssertEqual(span, expected, actual);
            }

            public void AssertReachedEndOfBaseline()
            {
                // Since we're walking the list of classified spans there's the chance that our baseline is longer.
                Assert.True(_baseline.Length == _index, $"Not all lines of the baseline were visited! {_baseline.Length} {_index}");
            }

            private void AssertEqual(TagHelperSpanInternal span, string expected, string actual)
            {
                if (string.Equals(expected, actual))
                {
                    return;
                }

                if (expected == null)
                {
                    var message = "The span is missing from baseline.";
                    throw new TagHelperSpanBaselineException(span, expected, actual, message);
                }
                else
                {
                    var message = $"Contents are not equal.";
                    throw new TagHelperSpanBaselineException(span, expected, actual, message);
                }
            }

            private class TagHelperSpanBaselineException : XunitException
            {
                public TagHelperSpanBaselineException(TagHelperSpanInternal span, string expected, string actual, string userMessage)
                    : base(Format(span, expected, actual, userMessage))
                {
                    Span = span;
                    Expected = expected;
                    Actual = actual;
                }

                public TagHelperSpanInternal Span { get; }

                public string Actual { get; }

                public string Expected { get; }

                private static string Format(TagHelperSpanInternal span, string expected, string actual, string userMessage)
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

                    return builder.ToString();
                }
            }
        }
    }
}
