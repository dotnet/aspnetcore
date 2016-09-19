// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal class RemoveCommand : ICommand
    {
        private readonly string _keyName;

        public static void Configure(CommandLineApplication command, CommandLineOptions options)
        {
            command.Description = "Removes the specified user secret";
            command.HelpOption();

            var keyArg = command.Argument("[name]", "Name of the secret");
            command.OnExecute(() =>
            {
                if (keyArg.Value == null)
                {
                    throw new GracefulException("Missing parameter value for 'name'.\nUse the '--help' flag to see info.");
                }

                options.Command = new RemoveCommand(keyArg.Value);
            });
        }


        public RemoveCommand(string keyName)
        {
            _keyName = keyName;
        }

        public void Execute(SecretsStore store, ILogger logger)
        {
            if (!store.ContainsKey(_keyName))
            {
                logger.LogWarning(Resources.Error_Missing_Secret, _keyName);
            }
            else
            {
                store.Remove(_keyName);
                store.Save();
            }
        }
    }
}