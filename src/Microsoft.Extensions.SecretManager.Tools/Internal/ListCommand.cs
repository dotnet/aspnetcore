// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    internal class ListCommand : ICommand
    {
        public static void Configure(CommandLineApplication command, CommandLineOptions options)
        {
            command.Description = "Lists all the application secrets";
            command.HelpOption();

            command.OnExecute(() =>
            {
                options.Command = new ListCommand();
            });
        }

        public void Execute(SecretsStore store, ILogger logger)
        {
            if (store.Count == 0)
            {
                logger.LogInformation(Resources.Error_No_Secrets_Found);
            }
            else
            {
                foreach (var secret in store.AsEnumerable())
                {
                    logger.LogInformation(Resources.FormatMessage_Secret_Value_Format(secret.Key, secret.Value));
                }
            }
        }
    }
}