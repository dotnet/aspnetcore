// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.Extensions.ApiDescription.Client
{
    internal class ProjectOptions
    {
        public CommandOption Project { get; private set; }

        public CommandOption Framework { get; private set; }

        public CommandOption Configuration { get; private set; }

        public CommandOption Runtime { get; private set; }

        public CommandOption MSBuildProjectExtensionsPath { get; private set; }

        public void Configure(CommandLineApplication command)
        {
            Project = command.Option("-p|--project <PROJECT>", Resources.ProjectDescription);
            Framework = command.Option("--framework <FRAMEWORK>", Resources.FrameworkDescription);
            Configuration = command.Option("--configuration <CONFIGURATION>", Resources.ConfigurationDescription);
            Runtime = command.Option("--runtime <RUNTIME_IDENTIFIER>", Resources.RuntimeDescription);
            MSBuildProjectExtensionsPath = command.Option("--msbuildprojectextensionspath <PATH>", Resources.ProjectExtensionsDescription);
        }
    }
}
