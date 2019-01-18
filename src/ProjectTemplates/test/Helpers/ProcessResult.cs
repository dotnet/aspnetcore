// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Templates.Test
{
    public readonly struct ProcessResult
    {
        public ProcessResult(ProcessStartInfo testProcessStartInfo, int exitCode, string output, string serverOutput)
        {
            TestProcessStartInfo = testProcessStartInfo;
            ExitCode = exitCode;
            Output = output;
            ServerOutput = serverOutput;
        }
        
        public ProcessStartInfo TestProcessStartInfo { get; }
        public int ExitCode { get; }
        public string ServerOutput { get; }
        public string Output { get; }
    }
}
