// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Commands;

namespace TriageBuildFailures
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var application = new CommandLineApplication();

            new RootCommand().Configure(application);

            application.Execute(args);
        }
    }
}
