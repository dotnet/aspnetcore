// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public readonly struct LinePragma : IEquatable<LinePragma>
{
    public LinePragma(int startLineIndex, int lineCount, string filePath)
        : this(startLineIndex: startLineIndex, lineCount: lineCount, filePath: filePath, startCharacterIndex: null, endCharacterIndex: null, characterOffset: null)
    {
    }

    public LinePragma(int startLineIndex, int lineCount, string filePath, int? startCharacterIndex, int? endCharacterIndex, int? characterOffset)
    {
        if (startLineIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startLineIndex));
        }

        if (lineCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineCount));
        }

        StartLineIndex = startLineIndex;
        LineCount = lineCount;
        FilePath = filePath;
        StartCharacterIndex = startCharacterIndex;
        EndCharacterIndex = endCharacterIndex;
        CharacterOffset = characterOffset;
    }

    public int StartLineIndex { get; }

    public int EndLineIndex => StartLineIndex + LineCount;

    public int LineCount { get; }

    public int? StartCharacterIndex { get; }

    public int? EndCharacterIndex { get; }

    public int? CharacterOffset { get; }

    public string FilePath { get; }

    public override bool Equals(object obj)
    {
        return obj is LinePragma other ? Equals(other) : false;
    }

    public bool Equals(LinePragma other)
    {
        return StartLineIndex == other.StartLineIndex &&
            LineCount == other.LineCount &&
            string.Equals(FilePath, other.FilePath, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        var hash = HashCodeCombiner.Start();
        hash.Add(StartLineIndex);
        hash.Add(LineCount);
        hash.Add(FilePath);
        return hash;
    }

    public override string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture, "Line index {0}, Count {1} - {2}", StartLineIndex, LineCount, FilePath);
    }
}
