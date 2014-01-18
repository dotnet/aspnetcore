// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.TestCommon;

namespace Microsoft.AspNet.Razor.Test.Utils
{
    public static class EventAssert
    {
        public static void NoMoreSpans(IEnumerator<Span> enumerator)
        {
            IList<Span> tokens = new List<Span>();
            while (enumerator.MoveNext())
            {
                tokens.Add(enumerator.Current);
            }

            Assert.False(tokens.Count > 0, String.Format(CultureInfo.InvariantCulture, @"There are more tokens available from the source: {0}", FormatList(tokens)));
        }

        private static string FormatList<T>(IList<T> items)
        {
            StringBuilder tokenString = new StringBuilder();
            foreach (T item in items)
            {
                tokenString.AppendLine(item.ToString());
            }
            return tokenString.ToString();
        }

        public static void NextSpanIs(IEnumerator<Span> enumerator, SpanKind type, string content, SourceLocation location)
        {
            Assert.True(enumerator.MoveNext(), "There is no next token!");
            IsSpan(enumerator.Current, type, content, location);
        }

        public static void NextSpanIs(IEnumerator<Span> enumerator, SpanKind type, string content, int actualIndex, int lineIndex, int charIndex)
        {
            NextSpanIs(enumerator, type, content, new SourceLocation(actualIndex, lineIndex, charIndex));
        }

        public static void IsSpan(Span tok, SpanKind type, string content, int actualIndex, int lineIndex, int charIndex)
        {
            IsSpan(tok, type, content, new SourceLocation(actualIndex, lineIndex, charIndex));
        }

        public static void IsSpan(Span tok, SpanKind type, string content, SourceLocation location)
        {
            Assert.Equal(content, tok.Content);
            Assert.Equal(type, tok.Kind);
            Assert.Equal(location, tok.Start);
        }
    }
}
