// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class HelpCommandBase : CommandBase
    {
        public override void Configure(CommandLineApplication command)
        {
            base.Configure(command);

            command.HelpOption("-h|--help");
        }
    }
}
