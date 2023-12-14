// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.ApiDescription.Tool;

internal sealed class ProjectOptions
{
    public CommandOption AssemblyPath { get; private set; }

    public CommandOption AssetsFile { get; private set; }

    public CommandOption Platform { get; private set; }

    public CommandOption ProjectName { get; private set; }

    public CommandOption RuntimeFrameworkVersion { get; private set; }

    public CommandOption TargetFramework { get; private set; }

    public void Configure(CommandLineApplication command)
    {
        AssemblyPath = command.Option("--assembly <Path>", Resources.AssemblyDescription);
        AssetsFile = command.Option("--assets-file <Path>", Resources.AssetsFileDescription);
        TargetFramework = command.Option("--framework <FRAMEWORK>", Resources.TargetFrameworkDescription);
        Platform = command.Option("--platform <Target>", Resources.PlatformDescription);
        ProjectName = command.Option("--project <Name>", Resources.ProjectDescription);
        RuntimeFrameworkVersion = command.Option("--runtime <RUNTIME_IDENTIFIER>", Resources.RuntimeDescription);
    }

    public void Validate()
    {
        if (!AssemblyPath.HasValue())
        {
            throw new CommandException(Resources.FormatMissingOption(AssemblyPath.LongName));
        }

        if (!ProjectName.HasValue())
        {
            throw new CommandException(Resources.FormatMissingOption(ProjectName.LongName));
        }

        if (!TargetFramework.HasValue())
        {
            throw new CommandException(Resources.FormatMissingOption(TargetFramework.LongName));
        }
    }
}
