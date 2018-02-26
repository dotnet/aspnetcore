// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Blazor.Build.Cli.Commands
{
    internal class ServeDevHost
    {
        public static void Command(CommandLineApplication command, string[] args)
        {
            command.OnExecute(() =>
            {
                var remainingArgs = args.Skip(1).ToArray();
                DevHost.Server.Program.BuildWebHost(remainingArgs).Run();
                return 0;
            });
        }
    }
}
