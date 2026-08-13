// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class ProjectCommandLineApplication : CommandLineApplication
{
    public CommandOption ProjectOption { get; private set; }

    public CommandOption FileOption { get; private set; }

    public CommandOption OutputOption { get; private set; }

    public IReporter Reporter { get; private set; }

    public ProjectCommandLineApplication(IReporter reporter, bool throwOnUnexpectedArg = true, bool continueAfterUnexpectedArg = false, bool treatUnmatchedOptionsAsArguments = false)
        : base(throwOnUnexpectedArg, continueAfterUnexpectedArg, treatUnmatchedOptionsAsArguments)
    {
        ProjectOption = Option(
            "-p|--project",
            Resources.ProjectOption_Description,
            CommandOptionType.SingleValue);

        FileOption = Option(
            "--file",
            Resources.FileOption_Description,
            CommandOptionType.SingleValue);

        OutputOption = Option(
            "-o|--output",
            Resources.CreateCommand_OutputOption_Description,
            CommandOptionType.SingleValue);

        Reporter = reporter;
    }

    public ProjectCommandLineApplication Command(string name, Action<ProjectCommandLineApplication> configuration)
    {
        var command = new ProjectCommandLineApplication(Reporter) { Name = name, Parent = this };
        Commands.Add(command);
        configuration(command);
        return command;
    }

    public bool TryGetProjectOrFilePath(out string path, out bool isFileBasedApp)
    {
        path = null;
        isFileBasedApp = false;

        if (ProjectOption.HasValue() && FileOption.HasValue())
        {
            Reporter.Error(Resources.ProjectAndFileOptions_Error);
            return false;
        }

        if (FileOption.HasValue())
        {
            path = FileOption.Value();
            isFileBasedApp = true;
            return true;
        }

        path = ProjectOption.Value();
        return true;
    }
}
