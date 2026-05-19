// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

internal sealed class ClearCommand : ICommand
{
    public static void Configure(CommandLineApplication command, CommandLineOptions options)
    {
        command.Description = "Deletes all the application secrets";
        command.HelpOption();

        command.OnExecute(() =>
        {
            options.Command = new ClearCommand();
        });
    }

    public void Execute(CommandContext context)
    {
        context.SecretStore.Clear();
        context.SecretStore.Save();
    }
}
