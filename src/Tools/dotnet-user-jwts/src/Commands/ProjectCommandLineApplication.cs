// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class ProjectCommandLineApplication : CommandLineApplication
{
    public CommandOption ProjectOption { get; private set; }

    public ProjectCommandLineApplication(bool throwOnUnexpectedArg = true, bool continueAfterUnexpectedArg = false, bool treatUnmatchedOptionsAsArguments = false)
        : base(throwOnUnexpectedArg, continueAfterUnexpectedArg, treatUnmatchedOptionsAsArguments)
    {
        ProjectOption = Option(
            "-p|--project",
            "The path of the project to operate on. Defaults to the project in the current directory",
            CommandOptionType.SingleValue);

        Options.Add(ProjectOption);
    }
}
