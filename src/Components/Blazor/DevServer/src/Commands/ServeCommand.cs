// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.DevServer.Commands
{
    internal class ServeCommand : CommandLineApplication
    {
        public ServeCommand(CommandLineApplication parent)

            // We pass arbitrary arguments through to the ASP.NET Core configuration
            : base(throwOnUnexpectedArg: false) 
        {
            Parent = parent;
            
            Name = "serve";
            Description = "Serve requests to a Blazor application";

            HelpOption("-?|-h|--help");

            ApplicationPath = new CommandArgument()
            {
                Description = "Path to the client application dll",
                MultipleValues = false,
                Name = "<PATH>",
                ShowInHelpText = true
            };
            Arguments.Add(ApplicationPath);

            OnExecute(Execute);
        }

        public CommandArgument ApplicationPath { get; private set; }

        private int Execute()
        {
            if (string.IsNullOrWhiteSpace(ApplicationPath.Value))
            {
                throw new InvalidOperationException($"Invalid value for parameter '{nameof(ApplicationPath)}'. Value supplied: '{ApplicationPath.Value}'");
            }

            Server.Startup.ApplicationAssembly = ApplicationPath.Value;
            Server.Program.BuildWebHost(RemainingArguments.ToArray()).Run();
            return 0;
        }
    }
}
