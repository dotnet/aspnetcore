// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Blazor.Cli.Commands
{
    class ServeCommand
    {
        public static void Command(CommandLineApplication command)
        {
            var remainingArgs = command.RemainingArguments.ToArray();

            Server.Program.BuildWebHost(remainingArgs).Run();
        }
    }
}
