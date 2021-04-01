// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Hosting;
using DevServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.Web.DevServer.Commands
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
            DevServerProgram.BuildWebHost(RemainingArguments.ToArray()).Run();
            return 0;
        }
    }
}
