// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public sealed class LineMapping : IEquatable<LineMapping>
    {
        public LineMapping(SourceSpan originalSourceSpan, SourceSpan generatedSourceSpan)
        {
            OriginalSpan = originalSourceSpan;
            GeneratedSpan = generatedSourceSpan;
        }

        public SourceSpan OriginalSpan { get; }

        public SourceSpan GeneratedSpan { get; }

        public override bool Equals(object obj)
        {
            var other = obj as LineMapping;
            return Equals(other);
        }

        public bool Equals(LineMapping other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return OriginalSpan.Equals(other.OriginalSpan) &&
                GeneratedSpan.Equals(other.GeneratedSpan);
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(OriginalSpan);
            hashCodeCombiner.Add(GeneratedSpan);

            return hashCodeCombiner;
        }

        public static bool operator ==(LineMapping left, LineMapping right)
        {
            if (ReferenceEquals(left, right))
            {
                // Exact equality e.g. both objects are null.
                return true;
            }

            if (ReferenceEquals(left, null))
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(LineMapping left, LineMapping right)
        {
            if (ReferenceEquals(left, right))
            {
                // Exact equality e.g. both objects are null.
                return false;
            }

            if (ReferenceEquals(left, null))
            {
                return true;
            }

            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} -> {1}", OriginalSpan, GeneratedSpan);
        }
    }
}
