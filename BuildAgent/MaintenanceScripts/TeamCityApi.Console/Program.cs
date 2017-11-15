// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.CommandLineUtils;
using TeamCityApi.Console.Commands;

namespace TeamCityApi.Console
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var application = new CommandLineApplication();

            new RootCommand().Configure(application);

            try
            {
                return application.Execute(args);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Exception thrown: '{ex.ToString()}'");
                return 1;
            }
        }
    }
}
