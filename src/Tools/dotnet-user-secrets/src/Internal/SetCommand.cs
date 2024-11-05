// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

internal sealed class SetCommand
{
    public static void Configure(CommandLineApplication command, CommandLineOptions options, IConsole console)
    {
        command.Description = "Sets the user secret to the specified value";
        command.ExtendedHelpText = @"
Additional Info:
  This command will also handle piped input. Piped input is expected to be a valid JSON format.

Examples:
  dotnet user-secrets set ConnStr ""User ID=bob;Password=***""
";

        var catCmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"type .\secrets.json"
            : "cat ./secrets.json";

        command.ExtendedHelpText += $@"  {catCmd} | dotnet user-secrets set";

        command.HelpOption();

        var nameArg = command.Argument("[name]", "Name of the secret");
        var valueArg = command.Argument("[value]", "Value of the secret");

        command.OnExecute(() =>
        {
            if (console.IsInputRedirected && nameArg.Value == null)
            {
                options.Command = new FromStdInStrategy();
            }
            else
            {
                if (string.IsNullOrEmpty(nameArg.Value))
                {
                    throw new CommandParsingException(command, Resources.FormatError_MissingArgument("name"));
                }

                if (valueArg.Value == null)
                {
                    throw new CommandParsingException(command, Resources.FormatError_MissingArgument("value"));
                }

                options.Command = new ForOneValueStrategy(nameArg.Value, valueArg.Value);
            }
        });
    }

    public sealed class FromStdInStrategy : ICommand
    {
        public void Execute(CommandContext context)
        {
            // parses stdin with the same parser that Microsoft.Extensions.Configuration.Json would use
            var provider = new ReadableJsonConfigurationProvider();
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.Unicode, 1024, true))
                {
                    writer.Write(context.Console.In.ReadToEnd()); // TODO buffer?
                }

                stream.Seek(0, SeekOrigin.Begin);
                provider.Load(stream);
            }

            foreach (var k in provider.CurrentData)
            {
                context.SecretStore.Set(k.Key, k.Value);
            }

            context.Reporter.Output(Resources.FormatMessage_Saved_Secrets(provider.CurrentData.Count));

            context.SecretStore.Save();
        }
    }

    public sealed class ForOneValueStrategy : ICommand
    {
        private readonly string _keyName;
        private readonly string _keyValue;

        public ForOneValueStrategy(string keyName, string keyValue)
        {
            _keyName = keyName;
            _keyValue = keyValue;
        }

        public void Execute(CommandContext context)
        {
            context.SecretStore.Set(_keyName, _keyValue);
            context.SecretStore.Save();
            context.Reporter.Output(Resources.FormatMessage_Saved_Secret(_keyName));
        }
    }
}
