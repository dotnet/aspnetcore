// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal readonly struct PathSegment : IEquatable<PathSegment>
    {
        public readonly int Start;
        public readonly int Length;

        public PathSegment(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public override bool Equals(object obj)
        {
            return obj is PathSegment segment ? Equals(segment) : false;
        }

        public bool Equals(PathSegment other)
        {
            return Start == other.Start && Length == other.Length;
        }

        public override int GetHashCode()
        {
            return Start;
        }

        public override string ToString()
        {
            return $"Segment({Start}:{Length})";
        }
    }
}
