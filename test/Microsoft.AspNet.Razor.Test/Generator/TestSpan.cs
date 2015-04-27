// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class TestSpan
    {
        /// <summary>
        /// Test span to simplify the generation of the actual Span in test initializer.
        /// </summary>
        /// <param name="kind">Span kind</param>
        /// <param name="start">Zero indexed start char index in the buffer.</param>
        /// <param name="end">End Column, if the text length is zero Start == End.</param>
        public TestSpan(SpanKind kind, int start, int end)
        {
            Kind = kind;
            Start = start;
            End = end;
        }

        public TestSpan(Span span)
            : this(span.Kind,
                   span.Start.AbsoluteIndex,
                   span.Start.AbsoluteIndex + span.Length)
        {
        }

        public SpanKind Kind { get; }

        public int Start { get; }

        public int End { get; }

        public override string ToString()
        {
            return string.Format("{0}: {1}-{2}", Kind, Start, End);
        }

        public override bool Equals(object obj)
        {
            var other = obj as TestSpan;
            return other != null &&
                Kind == other.Kind &&
                Start == other.Start &&
                End == other.End;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Kind)
                .Add(Start)
                .Add(End)
                .CombinedHash;
        }
    }
}
