// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;

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

            OnExecute(Execute);
        }

        private int Execute()
        {
            Server.Program.BuildWebHost(RemainingArguments.ToArray()).Run();
            return 0;
        }
    }
}
