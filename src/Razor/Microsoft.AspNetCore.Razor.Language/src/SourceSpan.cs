// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

public struct SourceSpan : IEquatable<SourceSpan>
{
    public static readonly SourceSpan Undefined = new SourceSpan(SourceLocation.Undefined, 0);

    public SourceSpan(int absoluteIndex, int length)
        : this(null, absoluteIndex, -1, -1, length)
    {
    }

    public SourceSpan(SourceLocation location, int contentLength)
        : this(location.FilePath, location.AbsoluteIndex, location.LineIndex, location.CharacterIndex, contentLength, lineCount: 1, endCharacterIndex: 0)
    {
    }

    public SourceSpan(string filePath, int absoluteIndex, int lineIndex, int characterIndex, int length)
        : this(filePath: filePath, absoluteIndex: absoluteIndex, lineIndex: lineIndex, characterIndex: characterIndex, length: length, lineCount: 0, endCharacterIndex: 0)
    {
    }

    public SourceSpan(string filePath, int absoluteIndex, int lineIndex, int characterIndex, int length, int lineCount, int endCharacterIndex)
    {
        AbsoluteIndex = absoluteIndex;
        LineIndex = lineIndex;
        CharacterIndex = characterIndex;
        Length = length;
        FilePath = filePath;
        LineCount = lineCount;
        EndCharacterIndex = endCharacterIndex;
    }

    public SourceSpan(int absoluteIndex, int lineIndex, int characterIndex, int length)
        : this(filePath: null, absoluteIndex: absoluteIndex, lineIndex: lineIndex, characterIndex: characterIndex, length: length)
    {
    }

    public int Length { get; }

    public int AbsoluteIndex { get; }

    public int LineIndex { get; }

    public int CharacterIndex { get; }

    public int LineCount { get; }

    public int EndCharacterIndex { get; }

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
        return obj is SourceSpan span && Equals(span);
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
        return left.Equals(right);
    }

    public static bool operator !=(SourceSpan left, SourceSpan right)
    {
        return !left.Equals(right);
    }
}
