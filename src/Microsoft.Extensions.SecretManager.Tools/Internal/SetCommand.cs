// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal class SetCommand : ICommand
    {
        private readonly string _keyName;
        private readonly string _keyValue;

        public static void Configure(CommandLineApplication command, CommandLineOptions options)
        {
            command.Description = "Sets the user secret to the specified value";
            command.HelpOption();

            var keyArg = command.Argument("[name]", "Name of the secret");
            var valueArg = command.Argument("[value]", "Value of the secret");

            command.OnExecute(() =>
            {
                if (keyArg.Value == null)
                {
                    throw new GracefulException("Missing parameter value for 'name'.\nUse the '--help' flag to see info.");
                }

                if (valueArg.Value == null)
                {
                    throw new GracefulException("Missing parameter value for 'value'.\nUse the '--help' flag to see info.");
                }

                options.Command = new SetCommand(keyArg.Value, valueArg.Value);
            });
        }

        public SetCommand(string keyName, string keyValue)
        {
            _keyName = keyName;
            _keyValue = keyValue;
        }

        public void Execute(SecretsStore store, ILogger logger)
        {
            store.Set(_keyName, _keyValue);
            store.Save();
            logger.LogInformation(Resources.Message_Saved_Secret, _keyName, _keyValue);
        }
    }
}