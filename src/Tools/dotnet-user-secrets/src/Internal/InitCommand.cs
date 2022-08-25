// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
/// <remarks>
/// Workaround used to handle the fact that the options have not been parsed at configuration time
/// </remarks>
public class InitCommandFactory : ICommand
{
    public CommandLineOptions Options { get; }

    internal static void Configure(CommandLineApplication command, CommandLineOptions options)
    {
        command.Description = "Set a user secrets ID to enable secret storage";
        command.HelpOption();

        command.OnExecute(() =>
        {
            options.Command = new InitCommandFactory(options);
        });
    }

    public InitCommandFactory(CommandLineOptions options)
    {
        Options = options;
    }

    public void Execute(CommandContext context)
    {
        new InitCommand(Options.Id, Options.Project).Execute(context);
    }

    public void Execute(CommandContext context, string workingDirectory)
    {
        new InitCommand(Options.Id, Options.Project).Execute(context, workingDirectory);
    }
}

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class InitCommand : ICommand
{
    public string OverrideId { get; }
    public string ProjectPath { get; }
    public string WorkingDirectory { get; private set; } = Directory.GetCurrentDirectory();

    public InitCommand(string id, string project)
    {
        OverrideId = id;
        ProjectPath = project;
    }

    public void Execute(CommandContext context, string workingDirectory)
    {
        WorkingDirectory = workingDirectory;
        Execute(context);
    }

    public void Execute(CommandContext context)
    {
        UserSecretsCreator.CreateUserSecretsId(context.Reporter, ProjectPath, WorkingDirectory, OverrideId);
    }
}
