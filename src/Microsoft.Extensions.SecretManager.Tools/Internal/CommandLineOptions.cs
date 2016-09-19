// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public class CommandLineOptions
    {
        public bool IsVerbose { get; set; }
        public bool IsHelp { get; set; }
        public string Project { get; set; }
        internal ICommand Command { get; set; }

        public static CommandLineOptions Parse(string[] args, TextWriter output)
        {
            var app = new CommandLineApplication()
            {
                Out = output,
                Name = "dotnet user-secrets",
                FullName = "User Secrets Manager",
                Description = "Manages user secrets"
            };

            app.HelpOption();
            app.VersionOption("--version", GetInformationalVersion());

            var optionVerbose = app.Option("-v|--verbose", "Verbose output",
                CommandOptionType.NoValue, inherited: true);

            var optionProject = app.Option("-p|--project <PROJECT>", "Path to project, default is current directory",
                CommandOptionType.SingleValue, inherited: true);

            var options = new CommandLineOptions();
            app.Command("set", c => SetCommand.Configure(c, options));
            app.Command("remove", c => RemoveCommand.Configure(c, options));
            app.Command("list", c => ListCommand.Configure(c, options));
            app.Command("clear", c => ClearCommand.Configure(c, options));

            // Show help information if no subcommand/option was specified.
            app.OnExecute(() => app.ShowHelp());

            if (app.Execute(args) != 0)
            {
                // when command line parsing error in subcommand
                return null;
            }

            options.IsHelp = app.IsShowingInformation;
            options.IsVerbose = optionVerbose.HasValue();
            options.Project = optionProject.Value();

            return options;
        }

        private static string GetInformationalVersion()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            var versionAttribute = attribute == null ?
                assembly.GetName().Version.ToString() :
                attribute.InformationalVersion;

            return versionAttribute;
        }
    }
}