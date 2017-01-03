// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public struct SourceSpan : IEquatable<SourceSpan>
    {
        public SourceSpan(SourceLocation location, int contentLength)
            : this(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, contentLength)
        {
        }

        public SourceSpan(string filePath, int absoluteIndex, int lineIndex, int characterIndex, int length)
        {
            AbsoluteIndex = absoluteIndex;
            LineIndex = lineIndex;
            CharacterIndex = characterIndex;
            Length = length;
            FilePath = filePath;
        }

        public int Length { get; }

        public int AbsoluteIndex { get; }

        public int LineIndex { get; }

        public int CharacterIndex { get; }

        public string FilePath { get; }

        public bool Equals(SourceSpan other)
        {
            return
                string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
                AbsoluteIndex == other.AbsoluteIndex &&
                LineIndex == other.LineIndex &&
                CharacterIndex == other.CharacterIndex &&
                Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            var other = obj as SourceSpan?;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return Equals(other.Value);
        }

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();
            hash.Add(FilePath, StringComparer.Ordinal);
            hash.Add(AbsoluteIndex);
            hash.Add(LineIndex);
            hash.Add(CharacterIndex);
            hash.Add(Length);

            return hash;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture, "({0}:{1},{2} [{3}] {4})",
                AbsoluteIndex,
                LineIndex,
                CharacterIndex,
                Length,
                FilePath);
        }

        public static bool operator ==(SourceSpan left, SourceSpan right)
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

        public static bool operator !=(SourceSpan left, SourceSpan right)
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
    }
}
