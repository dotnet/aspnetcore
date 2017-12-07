// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;

namespace Microsoft.Blazor.DevHost
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dotnet blazor <command>");
                return 1;
            }

            var command = args[0];
            var remainingArgs = args.Skip(1).ToArray();

            switch (command.ToLowerInvariant())
            {
                case "serve":
                    Server.Program.BuildWebHost(remainingArgs).Run();
                    return 0;
                default:
                    throw new InvalidOperationException($"Unknown command: {command}");
            }
        }
    }
}
