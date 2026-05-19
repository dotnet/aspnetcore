// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Analyzer.Testing;

/// <summary>
/// Location where the diagnostic appears, as determined by path, line number, and column number.
/// </summary>
public class DiagnosticLocation
{
    public DiagnosticLocation(int line, int column)
        : this($"{DiagnosticProject.DefaultFilePathPrefix}.cs", line, column)
    {
    }

    public DiagnosticLocation(string path, int line, int column)
    {
        if (line < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
        }

        if (column < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
        }

        Path = path;
        Line = line;
        Column = column;
    }

    public string Path { get; }
    public int Line { get; }
    public int Column { get; }
}
