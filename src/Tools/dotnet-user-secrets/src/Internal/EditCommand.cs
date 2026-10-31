// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

internal sealed class EditCommand : ICommand
{
    internal Action<string, List<string>> _runProcess = (fileName, arguments) =>
    {
        using var p = Process.Start(fileName, arguments);
        p.WaitForExit();
    };

    internal Func<string, string> _getEnvironmentVariable = Environment.GetEnvironmentVariable;

    public static void Configure(CommandLineApplication command, CommandLineOptions options)
    {
        command.Description = "Edits the application secrets";
        command.HelpOption();
        command.OnExecute(() => { options.Command = new EditCommand(); });
    }

    public void Execute(CommandContext context)
    {
        if (!File.Exists(context.SecretStore.SecretsFilePath))
        {
            context.SecretStore.Save();
        }

        var (editor, editorArgs) = GetEditorAndArgs();
        editorArgs.Add(context.SecretStore.SecretsFilePath);
        _runProcess(editor, editorArgs);
    }

    private (string, List<string>) GetEditorAndArgs()
    {
        // Check to see if the user specified an editor using environment variables.
        foreach (var envVar in new[] { "DOTNET_USER_SECRETS_EDITOR", "VISUAL", "EDITOR" })
        {
            var editor = _getEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(editor))
            {
                // Treat environment-provided editors as executable paths only. Platform defaults can provide arguments separately.
                return (editor, []);
            }
        }

        return GetPlatformDefaultEditorAndArgs();
    }

    internal static (string, List<string>) GetPlatformDefaultEditorAndArgs()
    {
        return OperatingSystem.IsWindows() ? ("notepad.exe", []) :
            OperatingSystem.IsMacOS() ? ("open", ["-t"]) :
            OperatingSystem.IsLinux() ? ("xdg-open", []) :
            ("vi", []);
    }
}
