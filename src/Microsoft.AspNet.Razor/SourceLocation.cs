// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Framework.Internal;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor
{
    /// <summary>
    /// A location in a Razor file.
    /// </summary>
#if NET45
    // No Serializable attribute in CoreCLR (no need for it anymore?)
    [Serializable]
#endif
    public struct SourceLocation : IEquatable<SourceLocation>, IComparable<SourceLocation>
    {
        /// <summary>
        /// An undefined <see cref="SourceLocation"/>.
        /// </summary>
        public static readonly SourceLocation Undefined =
            new SourceLocation(absoluteIndex: -1, lineIndex: -1, characterIndex: -1);

        /// <summary>
        /// A <see cref="SourceLocation"/> with <see cref="AbsoluteIndex"/>, <see cref="LineIndex"/>, and
        /// <see cref="CharacterIndex"/> initialized to 0.
        /// </summary>
        public static readonly SourceLocation Zero =
            new SourceLocation(absoluteIndex: 0, lineIndex: 0, characterIndex: 0);

        /// <summary>
        /// Initializes a new instance of <see cref="SourceLocation"/>.
        /// </summary>
        /// <param name="absoluteIndex">The absolute index.</param>
        /// <param name="lineIndex">The line index.</param>
        /// <param name="characterIndex">The character index.</param>
        public SourceLocation(int absoluteIndex, int lineIndex, int characterIndex)
            : this(filePath: null, absoluteIndex: absoluteIndex, lineIndex: lineIndex, characterIndex: characterIndex)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SourceLocation"/>.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="absoluteIndex">The absolute index.</param>
        /// <param name="lineIndex">The line index.</param>
        /// <param name="characterIndex">The character index.</param>
        public SourceLocation(string filePath, int absoluteIndex, int lineIndex, int characterIndex)
        {
            FilePath = filePath;
            AbsoluteIndex = absoluteIndex;
            LineIndex = lineIndex;
            CharacterIndex = characterIndex;
        }

        /// <summary>
        /// Path of the file.
        /// </summary>
        /// <remarks>When <c>null</c>, the parser assumes the location is in the file currently being processed.
        /// </remarks>
        public string FilePath { get; set; }

        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int AbsoluteIndex { get; set; }

        /// <summary>
        /// Gets the 1-based index of the line referred to by this Source Location.
        /// </summary>
        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int LineIndex { get; set; }

        /// <remarks>Set property is only accessible for deserialization purposes.</remarks>
        public int CharacterIndex { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "({0}:{1},{2})",
                AbsoluteIndex,
                LineIndex,
                CharacterIndex);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return (obj is SourceLocation) && Equals((SourceLocation)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // LineIndex and CharacterIndex can be calculated from AbsoluteIndex and the document content.
            return HashCodeCombiner.Start()
                .Add(FilePath, StringComparer.Ordinal)
                .Add(AbsoluteIndex)
                .CombinedHash;
        }

        /// <inheritdoc />
        public bool Equals(SourceLocation other)
        {
            return string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
                AbsoluteIndex == other.AbsoluteIndex &&
                LineIndex == other.LineIndex &&
                CharacterIndex == other.CharacterIndex;
        }

        /// <inheritdoc />
        public int CompareTo(SourceLocation other)
        {
            var filePathOrdinal = string.Compare(FilePath, other.FilePath, StringComparison.Ordinal);
            if (filePathOrdinal != 0)
            {
                return filePathOrdinal;
            }

            return AbsoluteIndex.CompareTo(other.AbsoluteIndex);
        }

        /// <summary>
        /// Advances the <see cref="SourceLocation"/> by the length of the <paramref name="text" />.
        /// </summary>
        /// <param name="left">The <see cref="SourceLocation"/> to advance.</param>
        /// <param name="text">The <see cref="string"/> to advance <paramref name="left"/> by.</param>
        /// <returns>The advanced <see cref="SourceLocation"/>.</returns>
        public static SourceLocation Advance(SourceLocation left, [NotNull] string text)
        {
            var tracker = new SourceLocationTracker(left);
            tracker.UpdateLocation(text);
            return tracker.CurrentLocation;
        }

        /// <summary>
        /// Adds two <see cref="SourceLocation"/>s.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>A <see cref="SourceLocation"/> that is the sum of the left and right operands.</returns>
        /// <exception cref="ArgumentException">if the <see cref="FilePath"/> of the left and right operands
        /// are different, and neither is null.</exception>
        public static SourceLocation operator +(SourceLocation left, SourceLocation right)
        {
            if (!string.Equals(left.FilePath, right.FilePath, StringComparison.Ordinal) &&
                left.FilePath != null &&
                right.FilePath != null)
            {
                // Throw if FilePath for left and right are different, and neither is null.
                throw new ArgumentException(
                    RazorResources.FormatSourceLocationFilePathDoesNotMatch(nameof(SourceLocation), "+"),
                    nameof(right));
            }

            var resultFilePath = left.FilePath ?? right.FilePath;
            if (right.LineIndex > 0)
            {
                // Column index doesn't matter
                return new SourceLocation(
                    resultFilePath,
                    left.AbsoluteIndex + right.AbsoluteIndex,
                    left.LineIndex + right.LineIndex,
                    right.CharacterIndex);
            }
            else
            {
                return new SourceLocation(
                    resultFilePath,
                    left.AbsoluteIndex + right.AbsoluteIndex,
                    left.LineIndex + right.LineIndex,
                    left.CharacterIndex + right.CharacterIndex);
            }
        }

        /// <summary>
        /// Subtracts two <see cref="SourceLocation"/>s.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>A <see cref="SourceLocation"/> that is the difference of the left and right operands.</returns>
        /// <exception cref="ArgumentException">if the <see cref="FilePath"/> of the left and right operands
        /// are different.</exception>
        public static SourceLocation operator -(SourceLocation left, SourceLocation right)
        {
            if (!string.Equals(left.FilePath, right.FilePath, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    RazorResources.FormatSourceLocationFilePathDoesNotMatch(nameof(SourceLocation), "-"),
                    nameof(right));
            }

            var characterIndex = left.LineIndex != right.LineIndex ?
                left.CharacterIndex : left.CharacterIndex - right.CharacterIndex;

            return new SourceLocation(
                filePath: null,
                absoluteIndex: left.AbsoluteIndex - right.AbsoluteIndex,
                lineIndex: left.LineIndex - right.LineIndex,
                characterIndex: characterIndex);
        }

        /// <summary>
        /// Determines whether the first operand is less than the second operand.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is less than <paramref name="right"/>.</returns>
        public static bool operator <(SourceLocation left, SourceLocation right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the first operand is greater than the second operand.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if <paramref name="left"/> is greater than <paramref name="right"/>.</returns>
        public static bool operator >(SourceLocation left, SourceLocation right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the operands are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
        public static bool operator ==(SourceLocation left, SourceLocation right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether the operands are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
        public static bool operator !=(SourceLocation left, SourceLocation right)
        {
            return !left.Equals(right);
        }
    }
}
