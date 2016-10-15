// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class ListCommand : ICommand
    {
        private readonly bool _jsonOutput;

        public static void Configure(CommandLineApplication command, CommandLineOptions options)
        {
            command.Description = "Lists all the application secrets";
            command.HelpOption();

            var optJson = command.Option("--json", "Use json output. JSON is wrapped by '//BEGIN' and '//END'",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new ListCommand(optJson.HasValue());
            });
        }

        public ListCommand(bool jsonOutput)
        {
            _jsonOutput = jsonOutput;
        }

        public void Execute(CommandContext context)
        {
            if (_jsonOutput)
            {
                ReportJson(context);
                return;
            }

            if (context.SecretStore.Count == 0)
            {
                context.Logger.LogInformation(Resources.Error_No_Secrets_Found);
            }
            else
            {
                foreach (var secret in context.SecretStore.AsEnumerable())
                {
                    context.Logger.LogInformation(Resources.FormatMessage_Secret_Value_Format(secret.Key, secret.Value));
                }
            }
        }

        private void ReportJson(CommandContext context)
        {
            var jObject = new JObject();
            foreach(var item in context.SecretStore.AsEnumerable())
            {
                jObject[item.Key] = item.Value;
            }

            // TODO logger would prefix each line.
            context.Console.Out.WriteLine("//BEGIN");
            context.Console.Out.WriteLine(jObject.ToString(Formatting.Indented));
            context.Console.Out.WriteLine("//END");
        }
    }
}