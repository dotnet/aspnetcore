// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Templates.Test.Helpers;

internal sealed class ProcessResult
{
    public ProcessResult(ProcessEx process)
    {
        Process = process.Process.StartInfo.FileName + " " + process.Process.StartInfo.Arguments;
        ExitCode = process.ExitCode;
        Output = process.Output;
        Error = process.Error;
    }

    public string Process { get; }

    public int ExitCode { get; set; }

    public string Error { get; }

    public string Output { get; }
}
