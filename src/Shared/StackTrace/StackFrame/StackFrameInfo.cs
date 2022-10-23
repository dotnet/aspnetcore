// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources;

internal sealed class StackFrameInfo
{
    public StackFrameInfo(int lineNumber, string? filePath, StackFrame? stackFrame, MethodDisplayInfo? methodDisplayInfo)
    {
        LineNumber = lineNumber;
        FilePath = filePath;
        StackFrame = stackFrame;
        MethodDisplayInfo = methodDisplayInfo;
    }

    public int LineNumber { get; }

    public string? FilePath { get; }

    public StackFrame? StackFrame { get; }

    public MethodDisplayInfo? MethodDisplayInfo { get; }
}
