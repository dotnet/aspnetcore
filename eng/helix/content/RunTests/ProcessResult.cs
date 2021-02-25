// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace RunTests
{
    public class ProcessResult
    {
        public ProcessResult(string standardOutput, string standardError, int exitCode)
        {
            StandardOutput = standardOutput;
            StandardError = standardError;
            ExitCode = exitCode;
        }

        public string StandardOutput { get; }
        public string StandardError { get; }
        public int ExitCode { get; }
    }
}
