// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal class ProjectOptions
    {
        public CommandOption Configuration { get; private set; }

        public CommandOption Project { get; private set; }

        public CommandOption ProjectExtensionsPath { get; private set; }

        public CommandOption Runtime { get; private set; }

        public CommandOption TargetFramework { get; private set; }

        public void Configure(CommandLineApplication command)
        {
            Configuration = command.Option("--configuration <CONFIGURATION>", Resources.ConfigurationDescription);
            Project = command.Option("-p|--project <PROJECT>", Resources.ProjectDescription);
            ProjectExtensionsPath = command.Option(
                "--projectExtensionsPath <PATH>",
                Resources.ProjectExtensionsPathDescription);
            Runtime = command.Option("--runtime <RUNTIME_IDENTIFIER>", Resources.RuntimeDescription);
            TargetFramework = command.Option("--framework <FRAMEWORK>", Resources.TargetFrameworkDescription);
        }
    }
}
