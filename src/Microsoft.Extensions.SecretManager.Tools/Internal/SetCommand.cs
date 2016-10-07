// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class SetCommand : ICommand
    {
        private readonly string _keyName;
        private readonly string _keyValue;

        public static void Configure(CommandLineApplication command, CommandLineOptions options)
        {
            command.Description = "Sets the user secret to the specified value";
            command.ExtendedHelpText = @"
Additional Info:
  This command will also handle piped input. Piped input is expected to be a valid JSON format.

Examples:
  dotnet user-secrets set ConnStr ""User ID=bob;Password=***""
  cat secrets.json | dotnet user-secrets set
";

            command.HelpOption();

            var nameArg = command.Argument("[name]", "Name of the secret");
            var valueArg = command.Argument("[value]", "Value of the secret");

            command.OnExecute(() =>
            {
                options.Command = new SetCommand(nameArg.Value, valueArg.Value);
            });
        }

        internal SetCommand(string keyName, string keyValue)
        {
            Debug.Assert(keyName != null || keyValue == null, "Inconsistent state. keyValue must not be null if keyName is null.");
            _keyName = keyName;
            _keyValue = keyValue;
        }

        internal SetCommand()
        { }

        public void Execute(CommandContext context)
        {
            if (context.Console.IsInputRedirected && _keyName == null)
            {
                ReadFromInput(context);
            }
            else
            {
                SetFromArguments(context);
            }
        }

        private void ReadFromInput(CommandContext context)
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

            context.Logger.LogInformation(Resources.Message_Saved_Secrets, provider.CurrentData.Count);

            context.SecretStore.Save();
        }

        private void SetFromArguments(CommandContext context)
        {
            if (_keyName == null)
            {
                throw new GracefulException(Resources.FormatError_MissingArgument("name"));
            }

            if (_keyValue == null)
            {
                throw new GracefulException((Resources.FormatError_MissingArgument("value")));
            }

            context.SecretStore.Set(_keyName, _keyValue);
            context.SecretStore.Save();
            context.Logger.LogInformation(Resources.Message_Saved_Secret, _keyName, _keyValue);
        }
    }
}