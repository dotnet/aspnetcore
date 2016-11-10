// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tools
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "dotnet publish-iis",
                FullName = "Asp.Net Core IIS Publisher",
                Description = "IIS Publisher for the Asp.Net Core web applications",
            };
            app.HelpOption("-h|--help");

            var publishFolderOption = app.Option("-p|--publish-folder", "The path to the publish output folder", CommandOptionType.SingleValue);
            var frameworkOption = app.Option("-f|--framework <FRAMEWORK>", "Target framework of application being published", CommandOptionType.SingleValue);
            var configurationOption = app.Option("-c|--configuration <CONFIGURATION>", "Target configuration of application being published", CommandOptionType.SingleValue);
            var projectPath = app.Argument("<PROJECT>", "The path to the project (project folder or project.json) being published. If empty the current directory is used.");

            app.OnExecute(() =>
            {
                var publishFolder = publishFolderOption.Value();
                var framework = frameworkOption.Value();

                if (publishFolder == null || framework == null)
                {
                    app.ShowHelp();
                    return 2;
                }

                Reporter.Output.WriteLine($"Configuring the following project for use with IIS: '{publishFolder}'");

                var exitCode = new PublishIISCommand(publishFolder, framework, configurationOption.Value(), projectPath.Value).Run();

                Reporter.Output.WriteLine("Configuring project completed successfully");

                return exitCode;
            });

            try
            {
                return app.Execute(args);
            }
            catch (Exception e)
            {
                Reporter.Error.WriteLine(e.Message.Red());
                Reporter.Output.WriteLine(e.ToString().Yellow());
            }

            return 1;
        }
    }
}
