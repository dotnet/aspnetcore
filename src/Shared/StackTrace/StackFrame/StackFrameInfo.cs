// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
#nullable enable

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class StackFrameInfo
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
}
