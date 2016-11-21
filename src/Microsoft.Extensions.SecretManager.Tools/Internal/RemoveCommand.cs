// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class RemoveCommand : ICommand
    {
        private readonly string _keyName;

        public static void Configure(CommandLineApplication command, CommandLineOptions options, IConsole console)
        {
            command.Description = "Removes the specified user secret";
            command.HelpOption();

            var keyArg = command.Argument("[name]", "Name of the secret");
            command.OnExecute(() =>
            {
                if (keyArg.Value == null)
                {
                    console.Error.WriteLine(Resources.FormatError_MissingArgument("name").Red());
                    return 1;
                }

                options.Command = new RemoveCommand(keyArg.Value);
                return 0;
            });
        }


        public RemoveCommand(string keyName)
        {
            _keyName = keyName;
        }

        public void Execute(CommandContext context)
        {
            if (!context.SecretStore.ContainsKey(_keyName))
            {
                context.Logger.LogWarning(Resources.Error_Missing_Secret, _keyName);
            }
            else
            {
                context.SecretStore.Remove(_keyName);
                context.SecretStore.Save();
            }
        }
    }
}