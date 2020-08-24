// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal class MSBuildResult
    {
        public MSBuildResult(ProjectDirectory project, string fileName, string arguments, int exitCode, string output)
        {
            Project = project;
            FileName = fileName;
            Arguments = arguments;
            ExitCode = exitCode;
            Output = output;
        }

        public ProjectDirectory Project { get; }

        public string Arguments { get; }

        public string FileName { get; }

        public int ExitCode { get; }

        public string Output { get; }
    }
}
