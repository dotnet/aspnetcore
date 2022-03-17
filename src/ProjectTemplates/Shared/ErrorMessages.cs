// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Templates.Test.Helpers;

internal static class ErrorMessages
{
    public static string GetFailedProcessMessage(string step, Project project, ProcessResult processResult)
    {
        return $@"Project {project.ProjectArguments} failed to {step}. Exit code {processResult.ExitCode}.
{processResult.Process}\nStdErr: {processResult.Error}\nStdOut: {processResult.Output}";
    }

    public static string GetFailedProcessMessageOrEmpty(string step, Project project, ProcessEx process)
    {
        return process.HasExited ? $@"Project {project.ProjectArguments} failed to {step}.
{process.GetFormattedOutput()}" : "";
    }
}
