// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;

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

        public SpanKind Kind { get; private set; }
        public int Start { get; private set; }
        public int End { get; private set; }

        public override string ToString()
        {
            return String.Format("{0}: {1}-{2}", Kind, Start, End);
        }

        public override bool Equals(object obj)
        {
            var other = obj as TestSpan;

            if (other != null)
            {
                return (Kind == other.Kind) &&
                       (Start == other.Start) &&
                       (End == other.End);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
