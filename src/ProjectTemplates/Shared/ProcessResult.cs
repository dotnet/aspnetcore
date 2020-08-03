// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;

namespace Templates.Test.Helpers
{
    internal class ProcessResult
    {
        public ProcessResult(ProcessEx process)
        {
            Process = process.Process.StartInfo.FileName + " " + process.Process.StartInfo.Arguments;
            ExitCode = process.ExitCode;
            Output = process.Output;
            Error = process.Error;
        }

        public string Process { get; }

        public int ExitCode { get; }

        public string Error { get; }

        public string Output { get; }
    }
}
