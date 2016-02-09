// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Tools.PublishIIS
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "dotnet publish-iis",
                FullName = "Asp.Net IIS Publisher",
                Description = "IIS Publisher for the Asp.Net web applications",
            };
            app.HelpOption("-h|--help");

            var publishFolderOption = app.Option("--publish-folder|-p", "The path to the publish output folder", CommandOptionType.SingleValue);
            var webRootOption = app.Option("--webroot|-w", "The name of webroot folder", CommandOptionType.SingleValue);
            var projectPath = app.Argument("<PROJECT>", "The path to the project (project folder or project.json) being published. If empty the current directory is used.");

            app.OnExecute(() =>
            {
                var publishFolder = publishFolderOption.Value();

                if (publishFolder == null)
                {
                    app.ShowHelp();
                    return 2;
                }

                Reporter.Output.WriteLine($"Configuring the following project for use with IIS: '{publishFolder}'");

                var exitCode = new PublishIISCommand(publishFolder, projectPath.Value, webRootOption.Value()).Run();

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
                Reporter.Verbose.WriteLine(e.ToString().Yellow());
            }

            return 1;
        }
    }
}
