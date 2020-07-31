// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.CommandLineUtils;
using Wasm.Performance.ConsoleHost.Scenarios;

namespace Wasm.Performance.ConsoleHost
{
    internal class Program : CommandLineApplication
    {
        static void Main(string[] args)
        {
            new Program().Execute(args);
        }

        public Program()
        {
            OnExecute(() =>
            {
                ShowHelp();
                return 1;
            });

            Commands.Add(new GridScenario());
        }
    }
}
