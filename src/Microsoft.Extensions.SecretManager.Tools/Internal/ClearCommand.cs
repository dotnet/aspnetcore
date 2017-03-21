// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal class ClearCommand : ICommand
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
}
