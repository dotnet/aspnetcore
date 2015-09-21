// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    public class MappingLocation
    {
        public MappingLocation()
        {
        }

        public MappingLocation(SourceLocation location, int contentLength)
        {
            ContentLength = contentLength;
            AbsoluteIndex = location.AbsoluteIndex;
            LineIndex = location.LineIndex;
            CharacterIndex = location.CharacterIndex;
        }

        public int ContentLength { get; }

        public int AbsoluteIndex { get; }

        public int LineIndex { get; }

        public int CharacterIndex { get; }

        public override bool Equals(object obj)
        {
            var other = obj as MappingLocation;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return AbsoluteIndex == other.AbsoluteIndex &&
                ContentLength == other.ContentLength &&
                LineIndex == other.LineIndex &&
                CharacterIndex == other.CharacterIndex;
        }

        public override int GetHashCode()
        {
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(AbsoluteIndex);
            hashCodeCombiner.Add(ContentLength);
            hashCodeCombiner.Add(LineIndex);
            hashCodeCombiner.Add(CharacterIndex);

            return hashCodeCombiner;
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture, "({0}:{1},{2} [{3}])",
                AbsoluteIndex,
                LineIndex,
                CharacterIndex,
                ContentLength);
        }

        public static bool operator ==(MappingLocation left, MappingLocation right)
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

        public static bool operator !=(MappingLocation left, MappingLocation right)
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
