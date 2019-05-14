// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace FunctionalTests
{
    public readonly struct ProcessResult
    {
        public ProcessResult(ProcessStartInfo processStartInfo, int exitCode, string output)
        {
            ProcessStartInfo = processStartInfo;
            ExitCode = exitCode;
            Output = output;
        }

        public ProcessStartInfo ProcessStartInfo { get; }
        public int ExitCode { get; }
        public string Output { get; }
    }
}
