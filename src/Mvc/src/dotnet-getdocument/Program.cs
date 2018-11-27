// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.Extensions.ApiDescription.Tool.Commands;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                FullName = Resources.CommandFullName,
            };

            new InvokeCommand().Configure(app);

            try
            {
                return app.Execute(args);
            }
            catch (Exception ex)
            {
                if (ex is CommandException || ex is CommandParsingException)
                {
                    Reporter.WriteVerbose(ex.ToString());
                }
                else
                {
                    Reporter.WriteInformation(ex.ToString());
                }

                Reporter.WriteError(ex.Message);

                return 1;
            }
        }
    }
}
