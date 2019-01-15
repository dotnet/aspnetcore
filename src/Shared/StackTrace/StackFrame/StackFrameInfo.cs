// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Extensions.StackTrace.Sources
{
    internal class StackFrameInfo
    {
        public int LineNumber { get; set; }

        public string FilePath { get; set; }

        public StackFrame StackFrame { get; set; }

        public MethodDisplayInfo MethodDisplayInfo { get; set; }
    }
}
