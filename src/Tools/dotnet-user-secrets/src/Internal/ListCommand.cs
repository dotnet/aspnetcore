// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

internal sealed class ListCommand : ICommand
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
            context.Reporter.Output(Resources.Error_No_Secrets_Found);
        }
        else
        {
            foreach (var secret in context.SecretStore.AsEnumerable())
            {
                context.Reporter.Output(Resources.FormatMessage_Secret_Value_Format(secret.Key, secret.Value));
            }
        }
    }

    private static void ReportJson(CommandContext context)
    {
        var jObject = new JObject();
        foreach (var item in context.SecretStore.AsEnumerable())
        {
            jObject[item.Key] = item.Value;
        }

        context.Reporter.Output("//BEGIN");
        context.Reporter.Output(jObject.ToString(Formatting.Indented));
        context.Reporter.Output("//END");
    }
}
