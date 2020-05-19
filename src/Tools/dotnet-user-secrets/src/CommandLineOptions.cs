// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.SecretManager.Tools
{
    public class CommandLineOptions
    {
        public ICommand Command { get; set; }
        public string Configuration { get; private set; }
        public string Id { get; private set; }
        public bool IsHelp { get; private set; }
        public bool IsVerbose { get; private set; }
        public string Project { get; private set; }

        public static CommandLineOptions Parse(string[] args, IConsole console)
        {
            var app = new CommandLineApplication(treatUnmatchedOptionsAsArguments: true)
            {
                Out = console.Out,
                Error = console.Error,
                Name = "dotnet user-secrets",
                FullName = "User Secrets Manager",
                Description = "Manages user secrets"
            };

            app.HelpOption();
            app.VersionOptionFromAssemblyAttributes(typeof(Program).GetTypeInfo().Assembly);

            var optionVerbose = app.VerboseOption();

            var optionProject = app.Option("-p|--project <PROJECT>", "Path to project. Defaults to searching the current directory.",
                CommandOptionType.SingleValue, inherited: true);

            var optionConfig = app.Option("-c|--configuration <CONFIGURATION>", $"The project configuration to use. Defaults to 'Debug'.",
                CommandOptionType.SingleValue, inherited: true);

            // the escape hatch if project evaluation fails, or if users want to alter a secret store other than the one
            // in the current project
            var optionId = app.Option("--id", "The user secret ID to use.",
                CommandOptionType.SingleValue, inherited: true);

            var options = new CommandLineOptions();

            app.Command("set", c => SetCommand.Configure(c, options, console));
            app.Command("remove", c => RemoveCommand.Configure(c, options));
            app.Command("list", c => ListCommand.Configure(c, options));
            app.Command("clear", c => ClearCommand.Configure(c, options));
            app.Command("init", c => InitCommandFactory.Configure(c, options));

            // Show help information if no subcommand/option was specified.
            app.OnExecute(() => app.ShowHelp());

            if (app.Execute(args) != 0)
            {
                // when command line parsing error in subcommand
                return null;
            }

            options.Configuration = optionConfig.Value();
            options.Id = optionId.Value();
            options.IsHelp = app.IsShowingInformation;
            options.IsVerbose = optionVerbose.HasValue();
            options.Project = optionProject.Value();

            return options;
        }
    }
}
