// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNet.Razor.Text
{
#if NET45
    // No Serializable attribute in CoreCLR (no need for it anymore?)
    [Serializable]
#endif
    public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
    {
        public static readonly SourceLocation Undefined = CreateUndefined();
        public static readonly SourceLocation Zero = new SourceLocation(0, 0, 0);

        public SourceLocation(int absoluteIndex, int lineIndex, int characterIndex)
        {
            AbsoluteIndex = absoluteIndex;
            LineIndex = lineIndex;
            CharacterIndex = characterIndex;
        }

        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int AbsoluteIndex { get; set; }

        /// <summary>
        /// Gets the 1-based index of the line referred to by this Source Location.
        /// </summary>
        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int LineIndex { get; set; }

        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int CharacterIndex { get; set; }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "({0}:{1},{2})", AbsoluteIndex, LineIndex, CharacterIndex);
        }

        public override bool Equals(object obj)
        {
            return (obj is SourceLocation) && Equals((SourceLocation)obj);
        }

        public override int GetHashCode()
        {
            // LineIndex and CharacterIndex can be calculated from AbsoluteIndex and the document content.
            return AbsoluteIndex;
        }

        public bool Equals(SourceLocation other)
        {
            return AbsoluteIndex == other.AbsoluteIndex &&
                   LineIndex == other.LineIndex &&
                   CharacterIndex == other.CharacterIndex;
        }

        public int CompareTo(SourceLocation other)
        {
            return AbsoluteIndex.CompareTo(other.AbsoluteIndex);
        }

        public static SourceLocation Advance(SourceLocation left, string text)
        {
            var tracker = new SourceLocationTracker(left);
            tracker.UpdateLocation(text);
            return tracker.CurrentLocation;
        }

        public static SourceLocation Add(SourceLocation left, SourceLocation right)
        {
            if (right.LineIndex > 0)
            {
                // Column index doesn't matter
                return new SourceLocation(left.AbsoluteIndex + right.AbsoluteIndex, left.LineIndex + right.LineIndex, right.CharacterIndex);
            }
            else
            {
                return new SourceLocation(left.AbsoluteIndex + right.AbsoluteIndex, left.LineIndex + right.LineIndex, left.CharacterIndex + right.CharacterIndex);
            }
        }

        public static SourceLocation Subtract(SourceLocation left, SourceLocation right)
        {
            return new SourceLocation(left.AbsoluteIndex - right.AbsoluteIndex,
                                      left.LineIndex - right.LineIndex,
                                      left.LineIndex != right.LineIndex ? left.CharacterIndex : left.CharacterIndex - right.CharacterIndex);
        }

        private static SourceLocation CreateUndefined()
        {
            var sl = new SourceLocation();
            sl.AbsoluteIndex = -1;
            sl.LineIndex = -1;
            sl.CharacterIndex = -1;
            return sl;
        }

        public static bool operator <(SourceLocation left, SourceLocation right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(SourceLocation left, SourceLocation right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator ==(SourceLocation left, SourceLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SourceLocation left, SourceLocation right)
        {
            return !left.Equals(right);
        }

        public static SourceLocation operator +(SourceLocation left, SourceLocation right)
        {
            return Add(left, right);
        }

        public static SourceLocation operator -(SourceLocation left, SourceLocation right)
        {
            return Subtract(left, right);
        }
    }
}
