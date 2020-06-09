// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Internal;

namespace Templates.Test.Helpers
{
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
}
