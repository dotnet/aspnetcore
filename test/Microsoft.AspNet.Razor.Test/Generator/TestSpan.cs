// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
            TestSpan other = obj as TestSpan;

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
