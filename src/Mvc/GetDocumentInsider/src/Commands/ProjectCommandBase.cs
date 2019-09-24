// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal abstract class ProjectCommandBase : HelpCommandBase
    {
        public CommandOption AssemblyPath { get; private set; }

        public CommandOption ToolsDirectory { get; private set; }

        public override void Configure(CommandLineApplication command)
        {
            base.Configure(command);

            AssemblyPath = command.Option("-a|--assembly <PATH>", Resources.AssemblyDescription);
            ToolsDirectory = command.Option("--tools-directory <PATH>", Resources.ToolsDirectoryDescription);
        }

        protected override void Validate()
        {
            base.Validate();

            if (!AssemblyPath.HasValue())
            {
                throw new CommandException(Resources.FormatMissingOption(AssemblyPath.LongName));
            }

            if (!ToolsDirectory.HasValue())
            {
                throw new CommandException(Resources.FormatMissingOption(ToolsDirectory.LongName));
            }
        }
    }
}
